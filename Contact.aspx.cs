using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class Contact : Page
    {
        private const int MaxPerHourPerIp = 5;
        private static readonly TimeSpan MinGapBetweenMessages = TimeSpan.FromSeconds(30);
        private static readonly Regex PhoneRegex = new Regex(@"^[0-9+\-\s()]{7,20}$", RegexOptions.Compiled);

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            pnlSuccess.Visible = false;
            pnlError.Visible = false;

            // ---- Spam: honeypot. Real people never see this field. Pretend success so bots learn nothing. ----
            if (!string.IsNullOrWhiteSpace(txtWebsite.Text))
            {
                ShowSuccess();
                return;
            }

            // ---- Server-side validation (the old Page.IsValid was always true: there were no validators) ----
            string name = (txtName.Text ?? string.Empty).Trim();
            string email = (txtEmail.Text ?? string.Empty).Trim();
            string phone = (txtPhone.Text ?? string.Empty).Trim();
            string message = (txtMessage.Text ?? string.Empty).Trim();

            if (!ValidateInput(name, email, phone, message))
                return;

            // ---- Spam: rate limiting (per session and per IP) ----
            if (IsRateLimited(out string limitMessage))
            {
                ShowError(limitMessage);
                return;
            }

            bool demoMode = string.Equals(ConfigurationManager.AppSettings["ContactDemoMode"], "true", StringComparison.OrdinalIgnoreCase);
            bool sent = TrySendToEmailService(name, email, phone, message);

            // ContactDemoMode=true in web.config restores the old "always show success" behaviour for presentations.
            if (sent || demoMode)
            {
                RecordSend();
                ShowSuccess();

                txtName.Text = string.Empty;
                txtEmail.Text = string.Empty;
                txtPhone.Text = string.Empty;
                txtMessage.Text = string.Empty;
            }
            else
            {
                ShowError("We couldn't send your message right now. Please try again shortly, or call us on 084 833 1373.");
            }
        }

        // ------------------------------------------------------------------
        //  Validation
        // ------------------------------------------------------------------
        private bool ValidateInput(string name, string email, string phone, string message)
        {
            bool ok = true;
            errName.Visible = errEmail.Visible = errPhone.Visible = errMessage.Visible = false;

            if (name.Length < 2 || name.Length > 100)
            {
                errName.Text = "Please enter your name.";
                errName.Visible = true;
                ok = false;
            }

            if (!IsValidEmail(email))
            {
                errEmail.Text = "Please enter a valid email address.";
                errEmail.Visible = true;
                ok = false;
            }

            if (phone.Length > 0 && !PhoneRegex.IsMatch(phone))
            {
                errPhone.Text = "Use digits only, e.g. 082 123 4567.";
                errPhone.Visible = true;
                ok = false;
            }

            if (message.Length < 10)
            {
                errMessage.Text = "Please tell us a little more (at least 10 characters).";
                errMessage.Visible = true;
                ok = false;
            }
            else if (message.Length > 2000)
            {
                errMessage.Text = "Please keep your message under 2000 characters.";
                errMessage.Visible = true;
                ok = false;
            }

            return ok;
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
            try
            {
                MailAddress parsed = new MailAddress(email);
                return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase) && email.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        //  Rate limiting
        // ------------------------------------------------------------------
        private string IpCacheKey
        {
            get { return "contact-ip-" + (Request.UserHostAddress ?? "unknown"); }
        }

        private bool IsRateLimited(out string message)
        {
            message = null;

            if (Session["ContactLastSent"] is DateTime last && DateTime.UtcNow - last < MinGapBetweenMessages)
            {
                message = "Please wait a moment before sending another message.";
                return true;
            }

            int count = HttpRuntime.Cache[IpCacheKey] is int c ? c : 0;
            if (count >= MaxPerHourPerIp)
            {
                message = "You've sent several messages recently. Please call us on 084 833 1373 if it's urgent.";
                return true;
            }

            return false;
        }

        private void RecordSend()
        {
            Session["ContactLastSent"] = DateTime.UtcNow;

            int count = HttpRuntime.Cache[IpCacheKey] is int c ? c : 0;
            HttpRuntime.Cache.Insert(
                IpCacheKey, count + 1, null,
                DateTime.UtcNow.AddHours(1), System.Web.Caching.Cache.NoSlidingExpiration);
        }

        // ------------------------------------------------------------------
        //  Email service call
        // ------------------------------------------------------------------
        private bool TrySendToEmailService(string name, string email, string phone, string message)
        {
            string emailServiceUrl = ConfigurationManager.AppSettings["EmailServiceUrl"];
            if (string.IsNullOrEmpty(emailServiceUrl)) return false;

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var payload = new
                {
                    type = "contact",
                    name,
                    email,
                    phone,
                    message
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);

                // Synchronous WebClient prevents ASP.NET async lifecycle thread drops/redirects
                using (var webClient = new TimeoutWebClient(15000))
                {
                    webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                    webClient.UploadString(emailServiceUrl, "POST", jsonPayload);
                }
                return true;
            }
            catch (Exception ex)
            {
                // Log the detail for developers; the visitor only sees a friendly message.
                Debug.WriteLine("Contact Form Error: " + ex.Message);
                return false;
            }
        }

        /// <summary>WebClient has no timeout by default; a slow email service would hang the page.</summary>
        private sealed class TimeoutWebClient : WebClient
        {
            private readonly int _timeoutMs;
            public TimeoutWebClient(int timeoutMs) { _timeoutMs = timeoutMs; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request != null) request.Timeout = _timeoutMs;
                return request;
            }
        }

        // ------------------------------------------------------------------
        //  UI helpers
        // ------------------------------------------------------------------
        private void ShowSuccess()
        {
            pnlSuccess.Visible = true;
            pnlError.Visible = false;
        }

        private void ShowError(string message)
        {
            lblError.Text = Server.HtmlEncode(message);
            pnlError.Visible = true;
            pnlSuccess.Visible = false;
        }
    }
}