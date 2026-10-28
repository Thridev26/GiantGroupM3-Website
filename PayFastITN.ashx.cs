using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

namespace GiantGroupM3
{
    //<%@ WebHandler Language="C#" CodeBehind="PayFastITN.ashx.cs" Class="GiantGroupM3.PayFastITN" %>
    public class PayFastITN : IHttpHandler
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["DefaultConnection"]
                    .ConnectionString;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.TrySkipIisCustomErrors = true;   // <-- add this

            try
            {
                context.Response.ContentType = "text/plain";
                if (!string.Equals(
                    context.Request.HttpMethod, "POST",
                    StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 405;
                    context.Response.Write("POST required");
                    return;
                }

                bool sandbox = string.Equals(
                    ConfigurationManager.AppSettings["PayFastSandboxMode"],
                    "true", StringComparison.OrdinalIgnoreCase);

                if (!sandbox)
                    throw new InvalidOperationException(
                        "This handler is configured for Sandbox testing only.");

                string merchantId =
                    ConfigurationManager.AppSettings["PayFastMerchantId"];

                string passphrase =
                    ConfigurationManager.AppSettings["PayFastPassphrase"] ?? "";

                if (string.IsNullOrWhiteSpace(merchantId))
                    throw new ConfigurationErrorsException(
                        "PayFastMerchantId is missing.");

                var fields =
                    new List<KeyValuePair<string, string>>();

                foreach (string key in context.Request.Form.AllKeys)
                {
                    if (string.IsNullOrEmpty(key) ||
                        string.Equals(key, "signature",
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    fields.Add(new KeyValuePair<string, string>(
                        key, context.Request.Form[key] ?? ""));
                }

                string suppliedSignature =
                    context.Request.Form["signature"];

                string calculatedSignature =
     PayFastCheckout.GenerateSignature(fields, passphrase, skipEmpty: false);

                if (string.IsNullOrWhiteSpace(suppliedSignature) ||
                    !FixedTimeEquals(
                        suppliedSignature, calculatedSignature))
                    throw new InvalidOperationException(
                        "Invalid PayFast signature.");

                if (!string.Equals(
                    context.Request.Form["merchant_id"],
                    merchantId, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Unexpected merchant ID.");

                if (!IsPayFastIp(context.Request.UserHostAddress))
                    throw new InvalidOperationException(
                        "Notification source IP was not recognised.");

                string paymentStatus =
                    context.Request.Form["payment_status"];

                if (!string.Equals(
                    paymentStatus, "COMPLETE",
                    StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 200;
                    context.Response.Write("Notification not completed");
                    return;
                }

                string transactionId =
                    context.Request.Form["pf_payment_id"];

                Guid checkoutToken;

                if (string.IsNullOrWhiteSpace(transactionId) ||
                    !Guid.TryParseExact(
                        context.Request.Form["m_payment_id"], "N",
                        out checkoutToken))
                    throw new InvalidOperationException(
                        "Missing or invalid transaction reference.");

                decimal grossAmount;

                if (!decimal.TryParse(
                    context.Request.Form["amount_gross"],
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out grossAmount))
                    throw new InvalidOperationException(
                        "Invalid gross payment amount.");

                string validationBody = BuildFormBody(
      context.Request.Form);

                if (!ValidateWithPayFast(validationBody))
                    throw new InvalidOperationException(
                        "PayFast server validation failed.");

                if (!RecordVerifiedPayment(
                    checkoutToken, transactionId, grossAmount))
                    throw new InvalidOperationException(
                        "Payment did not match a pending checkout.");

                context.Response.StatusCode = 200;
                context.Response.Write("OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "PayFast ITN validation failed: {0}", ex);

                context.Response.StatusCode = 400;
                context.Response.Write("Notification rejected");
            }
        }

        private bool ValidateWithPayFast(string body)
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(
                "https://sandbox.payfast.co.za/eng/query/validate");

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;

            byte[] data = Encoding.UTF8.GetBytes(body);
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            using (var response =
                (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return string.Equals(
                    reader.ReadToEnd().Trim(), "VALID",
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool RecordVerifiedPayment(
            Guid token, string transactionId, decimal grossAmount)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (SqlTransaction transaction =
                    conn.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        int jobId;
                        decimal expectedAmount;
                        string status;
                        string existingTransactionId;

                        const string readSql = @"
                            SELECT JobID, ExpectedAmount, PaymentStatus,
                                   PayFastTransactionID
                            FROM dbo.PayFastPaymentIntent
                                WITH (UPDLOCK, ROWLOCK)
                            WHERE CheckoutToken = @token;";

                        using (var cmd = new SqlCommand(
                            readSql, conn, transaction))
                        {
                            cmd.Parameters.Add("@token",
                                SqlDbType.UniqueIdentifier).Value = token;

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new InvalidOperationException(
                                        "Checkout record not found.");

                                jobId = reader.GetInt32(0);
                                expectedAmount = reader.GetDecimal(1);
                                status = reader.GetString(2);
                                existingTransactionId =
                                    reader.IsDBNull(3) ? null : reader.GetString(3);
                            }
                        }

                        // A repeated notification for the same completed
                        // transaction is safe and must not insert again.
                        if (status == "Paid" &&
                            string.Equals(
                                existingTransactionId, transactionId,
                                StringComparison.Ordinal))
                        {
                            transaction.Commit();
                            return true;
                        }

                        if (status != "Pending")
                            throw new InvalidOperationException(
                                "Checkout is no longer pending.");

                        if (Math.Abs(expectedAmount - grossAmount) > 0.009m)
                            throw new InvalidOperationException(
                                "Payment amount does not match checkout.");

                        const string insertSql = @"
                            INSERT INTO dbo.Payment
                                (amountPaid, paymentMethod, jobID,
                                 referenceNumber)
                            VALUES
                                (@amount, 'PayFast', @jobID, @reference);";

                        using (var cmd = new SqlCommand(
                            insertSql, conn, transaction))
                        {
                            var amountParam = cmd.Parameters.Add(
                                "@amount", SqlDbType.Decimal);
                            amountParam.Precision = 10;
                            amountParam.Scale = 2;
                            amountParam.Value = expectedAmount;

                            cmd.Parameters.Add("@jobID",
                                SqlDbType.Int).Value = jobId;

                            cmd.Parameters.Add("@reference",
                                SqlDbType.VarChar, 100).Value = transactionId;

                            cmd.ExecuteNonQuery();
                        }

                        const string updateSql = @"
                            UPDATE dbo.PayFastPaymentIntent
                            SET PaymentStatus = 'Paid',
                                PayFastTransactionID = @transactionId,
                                CompletedAt = GETDATE()
                            WHERE CheckoutToken = @token
                              AND PaymentStatus = 'Pending';";

                        using (var cmd = new SqlCommand(
                            updateSql, conn, transaction))
                        {
                            cmd.Parameters.Add("@transactionId",
                                SqlDbType.VarChar, 100).Value = transactionId;

                            cmd.Parameters.Add("@token",
                                SqlDbType.UniqueIdentifier).Value = token;

                            if (cmd.ExecuteNonQuery() != 1)
                                throw new InvalidOperationException(
                                    "Could not complete checkout.");
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private static string BuildFormBody(
    System.Collections.Specialized.NameValueCollection form)
        {
            var parts = new List<string>();

            foreach (string key in form.AllKeys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                parts.Add(
                    HttpUtility.UrlEncode(key) + "=" +
                    HttpUtility.UrlEncode(form[key] ?? ""));
            }

            return string.Join("&", parts);
        }

        private static bool IsPayFastIp(string remoteAddress)
        {
            IPAddress suppliedIp;

            if (!IPAddress.TryParse(remoteAddress, out suppliedIp))
                return false;

            if (suppliedIp.IsIPv4MappedToIPv6)
                suppliedIp = suppliedIp.MapToIPv4();

            string[] hosts =
            {
                "www.payfast.co.za",
                "sandbox.payfast.co.za",
                "w1w.payfast.co.za",
                "w2w.payfast.co.za"
            };

            foreach (string host in hosts)
            {
                try
                {
                    foreach (IPAddress address in
                        Dns.GetHostAddresses(host))
                    {
                        IPAddress candidate = address;

                        if (candidate.IsIPv4MappedToIPv6)
                            candidate = candidate.MapToIPv4();

                        if (candidate.Equals(suppliedIp))
                            return true;
                    }
                }
                catch (SocketException)
                {
                    // Continue checking the other PayFast hosts.
                }
            }

            return false;
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int difference = 0;

            for (int i = 0; i < a.Length; i++)
                difference |= a[i] ^ b[i];

            return difference == 0;
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}