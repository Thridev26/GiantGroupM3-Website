using System;
using System.Configuration;
using System.Web;
namespace GiantGroupM3
{
    public class PaymentReceiptRetry : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            if (context.Request.HttpMethod != "POST") { context.Response.StatusCode = 405; return; }
            string expected = ConfigurationManager.AppSettings["PaymentReceiptRetryKey"];
            string provided = context.Request.Headers["X-Receipt-Retry-Key"];
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrEmpty(provided) ||
                !ConstantTimeEqual(expected, provided)) { context.Response.StatusCode = 403; return; }
            context.Response.Write("Attempted " + PaymentReceiptService.RetryFailed(20) + " receipt deliveries.");
        }
        private static bool ConstantTimeEqual(string a, string b)
        {
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
                diff |= (i < a.Length ? a[i] : 0) ^ (i < b.Length ? b[i] : 0);
            return diff == 0;
        }
        public bool IsReusable { get { return false; } }
    }
}
