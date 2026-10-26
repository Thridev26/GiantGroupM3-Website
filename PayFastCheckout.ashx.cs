using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.SessionState;

namespace GiantGroupM3
{
    public class PayFastCheckout : IHttpHandler, IRequiresSessionState
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
            context.Response.ContentType = "text/html; charset=utf-8";

            try
            {
                if (!context.Request.IsAuthenticated ||
                    context.Session == null ||
                    !string.Equals(
                        Convert.ToString(context.Session["UserRole"]),
                        "Client",
                        StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 401;
                    context.Response.Write("Please sign in as a client.");
                    return;
                }

                int clientId;
                int jobId;

                if (!int.TryParse(
                        Convert.ToString(context.Session["ClientID"]),
                        out clientId) ||
                    clientId <= 0 ||
                    !int.TryParse(context.Request.QueryString["jobId"], out jobId) ||
                    jobId <= 0)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Write("Invalid checkout request.");
                    return;
                }

                string mode = ConfigurationManager.AppSettings[
                    "PayFastSandboxMode"];

                if (!string.Equals(
                    mode, "true", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 500;
                    context.Response.Write(
                        "PayFast Sandbox mode must be enabled for testing.");
                    return;
                }

                string merchantId = RequiredSetting("PayFastMerchantId");
                string merchantKey = RequiredSetting("PayFastMerchantKey");
                string passphrase =
                    ConfigurationManager.AppSettings["PayFastPassphrase"] ?? "";

                decimal balance = GetOutstandingBalance(jobId, clientId);

                if (balance < 5.00m)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Write(
                        "This job has no payable balance of at least R5.00.");
                    return;
                }

                Guid checkoutToken = CreatePaymentIntent(
                    jobId, clientId, balance);

                string baseUrl =
                    context.Request.Url.GetLeftPart(UriPartial.Authority);

                string paymentsUrl = baseUrl +
                    VirtualPathUtility.ToAbsolute("~/Invoicing/Payments.aspx");

                var fields = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("merchant_id", merchantId),
                    new KeyValuePair<string, string>("merchant_key", merchantKey),
                    new KeyValuePair<string, string>("return_url",
                        paymentsUrl + "?payment=return"),
                    new KeyValuePair<string, string>("cancel_url",
                        paymentsUrl + "?payment=cancel"),
                    new KeyValuePair<string, string>("notify_url",
                        baseUrl + VirtualPathUtility.ToAbsolute("~/PayFastITN.ashx")),
                    new KeyValuePair<string, string>("m_payment_id",
                        checkoutToken.ToString("N")),
                    new KeyValuePair<string, string>("amount",
                        balance.ToString("0.00", CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>("item_name",
                        "Giant Group job " + jobId),
                    new KeyValuePair<string, string>("item_description",
                        "Outstanding balance for job " + jobId)
                };

                string signature = GenerateSignature(fields, passphrase);

                context.Response.Write(
                    "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
                    "<title>Redirecting to PayFast</title></head><body>" +
                    "<p>Redirecting you securely to PayFast Sandbox...</p>" +
                    "<form id=\"payfast\" method=\"post\" " +
                    "action=\"https://sandbox.payfast.co.za/eng/process\">");

                foreach (KeyValuePair<string, string> field in fields)
                {
                    context.Response.Write(
                        "<input type=\"hidden\" name=\"" +
                        HttpUtility.HtmlAttributeEncode(field.Key) +
                        "\" value=\"" +
                        HttpUtility.HtmlAttributeEncode(field.Value) +
                        "\">");
                }

                context.Response.Write(
                    "<input type=\"hidden\" name=\"signature\" value=\"" +
                    HttpUtility.HtmlAttributeEncode(signature) + "\">" +
                    "<button type=\"submit\">Continue to PayFast</button>" +
                    "</form><script>document.getElementById('payfast').submit();" +
                    "</script></body></html>");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "PayFast checkout failed: {0}", ex);

                context.Response.StatusCode = 500;
                context.Response.Write(
                    "Checkout could not be started. Please try again later.");
            }
        }

        private decimal GetOutstandingBalance(int jobId, int clientId)
        {
            const string sql = @"
                SELECT
                    CAST(q.amount - COALESCE(p.TotalPaid, 0)
                         AS decimal(10,2)) AS Balance
                FROM dbo.Job AS j
                INNER JOIN dbo.Quote AS q ON j.quoteID = q.QuoteID
                INNER JOIN dbo.JobRequest AS jr
                    ON q.jobRequestID = jr.jobRequestID
                OUTER APPLY
                (
                    SELECT SUM(amountPaid) AS TotalPaid
                    FROM dbo.Payment
                    WHERE jobID = j.jobID
                ) AS p
                WHERE j.jobID = @jobID
                  AND jr.clientID = @clientID;";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@jobID", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = clientId;

                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException(
                        "Job not found for this client.");

                return Convert.ToDecimal(
                    result, CultureInfo.InvariantCulture);
            }
        }

        private Guid CreatePaymentIntent(
            int jobId, int clientId, decimal amount)
        {
            const string sql = @"
                INSERT INTO dbo.PayFastPaymentIntent
                    (JobID, ClientID, ExpectedAmount, PaymentStatus)
                OUTPUT INSERTED.CheckoutToken
                VALUES
                    (@jobID, @clientID, @amount, 'Pending');";

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@jobID", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = clientId;

                var amountParam = cmd.Parameters.Add(
                    "@amount", SqlDbType.Decimal);
                amountParam.Precision = 10;
                amountParam.Scale = 2;
                amountParam.Value = amount;

                conn.Open();
                return (Guid)cmd.ExecuteScalar();
            }
        }

        private static string RequiredSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (string.IsNullOrWhiteSpace(value))
                throw new ConfigurationErrorsException(
                    "Missing appSetting: " + key);

            return value;
        }

        internal static string GenerateSignature(
            IList<KeyValuePair<string, string>> fields,
            string passphrase)
        {
            var parts = new List<string>();

            foreach (KeyValuePair<string, string> field in fields)
            {
                if (!string.IsNullOrEmpty(field.Value))
                {
                    parts.Add(
                        field.Key + "=" +
                        HttpUtility.UrlEncode(field.Value.Trim()));
                }
            }

            if (!string.IsNullOrWhiteSpace(passphrase))
            {
                parts.Add(
                    "passphrase=" +
                    HttpUtility.UrlEncode(passphrase.Trim()));
            }

            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(
                    string.Join("&", parts));

                byte[] hash = md5.ComputeHash(bytes);
                var result = new StringBuilder();

                foreach (byte b in hash)
                    result.Append(b.ToString("x2"));

                return result.ToString();
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}