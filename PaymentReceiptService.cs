using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace GiantGroupM3
{
    // Uses a separate Apps Script endpoint, NOT the existing OTP endpoint.
    public static class PaymentReceiptService
    {
        private static string Conn { get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; } }

        public static void Queue(SqlConnection conn, SqlTransaction tx, Guid checkoutToken, int jobId,
            string payFastId, decimal amount)
        {
            const string sql = @"
INSERT INTO dbo.PaymentReceiptQueue
 (CheckoutToken, JobID, PayFastTransactionID, AmountPaid, RecipientEmail, ClientName)
SELECT @token, @job, @pf, @amount, c.emailAddress,
       LTRIM(RTRIM(COALESCE(c.clientName, '') + ' ' + COALESCE(c.clientSurname, '')))
FROM dbo.Job j
JOIN dbo.Quote q ON q.quoteID = j.quoteID
JOIN dbo.JobRequest jr ON jr.jobRequestID = q.jobRequestID
JOIN dbo.Client c ON c.clientID = jr.clientID
WHERE j.jobID = @job
  AND NULLIF(LTRIM(RTRIM(c.emailAddress)), '') IS NOT NULL;";
            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.Add("@token", SqlDbType.UniqueIdentifier).Value = checkoutToken;
                cmd.Parameters.Add("@job", SqlDbType.Int).Value = jobId;
                cmd.Parameters.Add("@pf", SqlDbType.VarChar, 100).Value = payFastId;
                var p = cmd.Parameters.Add("@amount", SqlDbType.Decimal);
                p.Precision = 10; p.Scale = 2; p.Value = decimal.Round(amount, 2);
                if (cmd.ExecuteNonQuery() != 1)
                    System.Diagnostics.Trace.TraceWarning("Receipt not queued: missing client email for job " + jobId);
            }
        }

        public static void TrySend(Guid checkoutToken)
        {
            // This never throws to the payment notification handler.
            try { SendClaimed(checkoutToken); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Receipt dispatch: " + ex); }
        }

        // Called for a specific token immediately after successful payment commit.
        private static void SendClaimed(Guid checkoutToken)
        {
            string url = ConfigurationManager.AppSettings["PaymentReceiptScriptUrl"];
            string secret = ConfigurationManager.AppSettings["PaymentReceiptSharedSecret"];
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(secret))
                throw new ConfigurationErrorsException("Receipt script URL or secret missing.");

            Receipt record = null;
            using (var conn = new SqlConnection(Conn))
            {
                conn.Open();
                const string claim = @"
UPDATE dbo.PaymentReceiptQueue
SET Status = 'Sending', Attempts = Attempts + 1, LastAttemptAt = GETUTCDATE()
OUTPUT inserted.CheckoutToken, inserted.JobID, inserted.PayFastTransactionID,
       inserted.AmountPaid, inserted.RecipientEmail, inserted.ClientName,
       inserted.CreatedAt
WHERE CheckoutToken = @token AND Status IN ('Pending','Failed');";
                using (var cmd = new SqlCommand(claim, conn))
                {
                    cmd.Parameters.Add("@token", SqlDbType.UniqueIdentifier).Value = checkoutToken;
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) record = new Receipt {
                            Token = r.GetGuid(0), JobId = r.GetInt32(1), PaymentId = r.GetString(2),
                            Amount = r.GetDecimal(3), Email = r.GetString(4), Name = r.GetString(5),
                            Date = r.GetDateTime(6)
                        };
                    }
                }
            }
            if (record == null) return;

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                var json = new JavaScriptSerializer().Serialize(new {
                    type = "payment_receipt", secret = secret, email = record.Email,
                    name = record.Name, jobId = record.JobId,
                    paymentId = record.PaymentId,
                    amount = record.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                    paidAt = record.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                });
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                var bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length);
                string responseText;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                    responseText = reader.ReadToEnd();
                var result = new JavaScriptSerializer().DeserializeObject(responseText) as System.Collections.Generic.Dictionary<string, object>;
                if (result == null || !result.ContainsKey("ok") || !object.Equals(result["ok"], true))
                    throw new InvalidOperationException("Receipt script rejected the request: " + responseText);
                SetStatus(record.Token, "Sent", null);
            }
            catch (Exception ex)
            {
                SetStatus(record.Token, "Failed", ex.Message);
                throw;
            }
        }

        // Retry failures using the separately secured handler. Bounded to prevent infinite sends.
        public static int RetryFailed(int maxCount)
        {
            var tokens = new System.Collections.Generic.List<Guid>();
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
SELECT TOP (@count) CheckoutToken FROM dbo.PaymentReceiptQueue
WHERE Status IN ('Pending','Failed') AND Attempts < 5
AND (LastAttemptAt IS NULL OR LastAttemptAt < DATEADD(MINUTE, -10, GETUTCDATE()))
ORDER BY CreatedAt", conn))
            {
                cmd.Parameters.Add("@count", SqlDbType.Int).Value = Math.Min(Math.Max(maxCount, 1), 30);
                conn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) tokens.Add(r.GetGuid(0));
            }
            foreach (var token in tokens) TrySend(token);
            return tokens.Count;
        }

        private static void SetStatus(Guid token, string status, string error)
        {
            using (var conn = new SqlConnection(Conn))
            using (var cmd = new SqlCommand(@"
UPDATE dbo.PaymentReceiptQueue SET Status = @status,
SentAt = CASE WHEN @status = 'Sent' THEN GETUTCDATE() ELSE SentAt END,
LastError = @error WHERE CheckoutToken = @token AND Status = 'Sending';", conn))
            {
                cmd.Parameters.Add("@status", SqlDbType.VarChar, 12).Value = status;
                cmd.Parameters.Add("@error", SqlDbType.NVarChar, 1000).Value =
                    (object)(error == null ? null : error.Substring(0, Math.Min(error.Length, 1000))) ?? DBNull.Value;
                cmd.Parameters.Add("@token", SqlDbType.UniqueIdentifier).Value = token;
                conn.Open(); cmd.ExecuteNonQuery();
            }
        }

        private sealed class Receipt
        {
            public Guid Token; public int JobId; public string PaymentId;
            public decimal Amount; public string Email; public string Name; public DateTime Date;
        }
    }
}
