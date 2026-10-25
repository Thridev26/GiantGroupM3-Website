using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class Login : Page
    {
        private const int MaxLoginFailures = 5;      // per email + IP, per 15 minutes
        private const int MaxMfaAttempts = 5;        // wrong codes before the user must sign in again
        private const int MaxMfaResends = 3;

        // ---------------------------------------------------------------
        // Page load
        // ---------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            RegisterHyperLink.NavigateUrl = "Register";
            var returnUrl = HttpUtility.UrlEncode(Request.QueryString["ReturnUrl"]);
            if (!String.IsNullOrEmpty(returnUrl))
            {
                RegisterHyperLink.NavigateUrl += "?ReturnUrl=" + returnUrl;
            }

            // "Resend code" link posts back with the event target "mfaResend"
            if (IsPostBack && Request["__EVENTTARGET"] == "mfaResend")
            {
                ResendMfaCode();
                return;
            }

            if (!IsPostBack)
            {
                // Registration success message (only on first load, so it never hides a later error)
                if (Request.QueryString["registered"] == "true")
                {
                    ShowMessage("Registration successful! Please log in with your credentials.", true);
                }

                // Google sends the user back here with ?code=...
                CheckGoogleAuthCallback();
            }
        }

        // ---------------------------------------------------------------
        // Step 1: email + password
        // ---------------------------------------------------------------
        protected void LogIn(object sender, EventArgs e)
        {
            if (!IsValid) return;

            string email = Email.Text.Trim();
            string password = Password.Text.Trim();

            // Brute-force protection: too many failures for this email/IP in the last 15 minutes
            string throttleKey = "loginfail:" + Request.UserHostAddress + ":" + email.ToLowerInvariant();
            if (IsLockedOut(throttleKey))
            {
                ShowMessage("Too many unsuccessful attempts. Please wait 15 minutes and try again, or reset your password.", false);
                return;
            }

            string userType, userName;
            int userLevel, userId;

            if (ValidateStaffOrClientCredentials(email, password, out userType, out userName, out userLevel, out userId))
            {
                HttpRuntime.Cache.Remove(throttleKey);
                try
                {
                    IssueOtpAndShowMfa(email, userType, userName, userLevel, userId, RememberMe.Checked);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("MFA dispatch failed: " + ex);
                    ShowMessage("We couldn't send your verification code. Please try again in a moment.", false);
                }
            }
            else
            {
                RegisterFailure(throttleKey);
                ShowMessage("Invalid email or password.", false);
            }
        }

        // ---------------------------------------------------------------
        // Step 2: one-time code
        // ---------------------------------------------------------------
        protected void VerifyMfa_Click(object sender, EventArgs e)
        {
            string enteredCode = MfaCode.Text.Trim();
            string storedCode = Session["MfaOtpCode"] as string;
            DateTime? expiry = Session["MfaExpiry"] as DateTime?;
            string email = Session["PendingEmail"] as string;

            // No pending sign-in (session expired or forged postback): start again
            if (email == null || storedCode == null)
            {
                BackToLoginForm("Your verification session has expired. Please sign in again.");
                return;
            }

            int attempts = (Session["MfaAttempts"] as int?) ?? 0;
            if (attempts >= MaxMfaAttempts)
            {
                ClearPendingSession();
                BackToLoginForm("Too many incorrect codes. Please sign in again.");
                return;
            }

            // The demo code is OFF unless <add key="AllowDemoMfaCode" value="true" /> is set in Web.config.
            // Never enable it on the live site.
            bool demoEnabled = string.Equals(ConfigurationManager.AppSettings["AllowDemoMfaCode"], "true", StringComparison.OrdinalIgnoreCase);
            bool isDemoFallback = demoEnabled && enteredCode == "123456";
            bool isValidRealCode = expiry.HasValue && DateTime.Now <= expiry.Value && ConstantTimeEquals(enteredCode, storedCode);

            if (!(isDemoFallback || isValidRealCode))
            {
                Session["MfaAttempts"] = attempts + 1;
                int left = MaxMfaAttempts - (attempts + 1);
                ShowMessage(left > 0
                    ? "Invalid or expired verification code. You have " + left + " attempt" + (left == 1 ? "" : "s") + " left."
                    : "Invalid or expired verification code.", false);
                return;
            }

            // MFA success: finalise the login session
            string userType = Session["PendingUserType"] as string;      // "Staff" or "Client"
            int pendingUserId = Convert.ToInt32(Session["PendingUserId"]);
            string pendingName = Session["PendingUserName"] as string;

            Session["StaffName"] = pendingName;
            Session["Username"] = pendingName;                           // used by the portals' welcome text
            Session["UserType"] = userType;
            Session["UserRole"] = userType;                              // lets the master page IsClient() work
            Session["UserLevel"] = Session["PendingUserLevel"];

            if (userType.Equals("Client", StringComparison.OrdinalIgnoreCase))
            {
                Session["ClientID"] = pendingUserId;
            }
            else
            {
                Session["StaffID"] = pendingUserId;
                Session["AccessLevel"] = Session["PendingUserLevel"];    // used by the Assign / Allocation pages
            }

            bool rememberMe = Session["PendingRememberMe"] != null && (bool)Session["PendingRememberMe"];
            ClearPendingSession();

            FormsAuthentication.SetAuthCookie(email, rememberMe);

            // Honour ReturnUrl only if it is a local path (blocks open-redirect phishing links)
            string returnUrl = Request.QueryString["ReturnUrl"];
            if (IsLocalUrl(returnUrl))
            {
                Response.Redirect(returnUrl);
                return;
            }

            string role = Session["UserRole"]?.ToString() ?? string.Empty;
            int userLevel = Convert.ToInt32(Session["UserLevel"] ?? -1);

            if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                userLevel >= 2)
            {
                Response.Redirect("~/ManagementPortal.aspx");
            }
            else if (role.Equals("Client", StringComparison.OrdinalIgnoreCase) || userLevel == 0 || userLevel == 1)
            {
                Response.Redirect("~/ClientPortal.aspx");
            }
            else
            {
                Response.Redirect("~/Default.aspx");
            }
        }

        private void ResendMfaCode()
        {
            string email = Session["PendingEmail"] as string;
            if (email == null)
            {
                BackToLoginForm("Your verification session has expired. Please sign in again.");
                return;
            }

            int resends = (Session["MfaResends"] as int?) ?? 0;
            DateTime? last = Session["MfaLastSent"] as DateTime?;
            if (resends >= MaxMfaResends)
            {
                ShowMessage("You have requested too many codes. Please sign in again.", false);
                return;
            }
            if (last.HasValue && (DateTime.Now - last.Value).TotalSeconds < 30)
            {
                ShowMessage("Please wait a few seconds before requesting another code.", false);
                return;
            }

            try
            {
                IssueOtpAndShowMfa(email,
                    Session["PendingUserType"] as string,
                    Session["PendingUserName"] as string,
                    Convert.ToInt32(Session["PendingUserLevel"]),
                    Convert.ToInt32(Session["PendingUserId"]),
                    Session["PendingRememberMe"] != null && (bool)Session["PendingRememberMe"]);
                Session["MfaResends"] = resends + 1;
                ShowMessage("A new verification code has been sent.", true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("MFA resend failed: " + ex);
                ShowMessage("We couldn't send a new code. Please try again shortly.", false);
            }
        }

        /// <summary>
        /// Creates a one-time code, stores it (database + session), emails it, and switches to the MFA panel.
        /// Shared by password login, Google login and "Resend code".
        /// </summary>
        private void IssueOtpAndShowMfa(string email, string userType, string userName, int userLevel, int userId, bool rememberMe)
        {
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string otp = GenerateOtp();
            DateTime expiryTime = DateTime.Now.AddMinutes(5);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // Clean up expired records older than 24 hours
                using (SqlCommand cmdCleanup = new SqlCommand("DELETE FROM PasswordResets WHERE ExpiryTime < DATEADD(hour, -24, GETDATE());", conn))
                {
                    cmdCleanup.ExecuteNonQuery();
                }

                // Confirm against the database whether this email belongs to Staff
                bool isActuallyStaff;
                using (SqlCommand staffCheckCmd = new SqlCommand("SELECT COUNT(1) FROM staff WHERE emailAddress = @Email", conn))
                {
                    staffCheckCmd.Parameters.AddWithValue("@Email", email);
                    isActuallyStaff = Convert.ToInt32(staffCheckCmd.ExecuteScalar()) > 0;
                }

                bool staffRoute = isActuallyStaff || (userType != null &&
                    (userType.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                     userType.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
                     userType.Equals("Admin", StringComparison.OrdinalIgnoreCase)));

                string insertQuery = staffRoute
                    ? "INSERT INTO PasswordResets (StaffID, OTP, ExpiryTime) VALUES (@UserId, @Otp, DATEADD(minute, 5, GETDATE()))"
                    : "INSERT INTO PasswordResets (ClientID, OTP, ExpiryTime) VALUES (@UserId, @Otp, DATEADD(minute, 5, GETDATE()))";

                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@Otp", otp);
                    cmd.ExecuteNonQuery();
                }
            }

            // Dispatch via the Google Apps Script web app. JSON is serialised (never string-concatenated).
            string webAppUrl = ConfigurationManager.AppSettings["EmailServiceUrl"];
            if (string.IsNullOrEmpty(webAppUrl)) throw new InvalidOperationException("EmailServiceUrl is not configured.");

            string jsonData = new JavaScriptSerializer().Serialize(new { email = email, otp = otp, type = "login" });
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.UploadString(webAppUrl, "POST", jsonData);
            }

            // Hold the pending sign-in until the code is verified
            Session["PendingEmail"] = email;
            Session["PendingUserType"] = userType;
            Session["PendingUserName"] = userName;
            Session["PendingUserLevel"] = userLevel;
            Session["PendingUserId"] = userId;
            Session["PendingRememberMe"] = rememberMe;
            Session["MfaOtpCode"] = otp;
            Session["MfaExpiry"] = expiryTime;
            Session["MfaAttempts"] = 0;
            Session["MfaLastSent"] = DateTime.Now;

            loginForm.Visible = false;
            MfaPanel.Visible = true;
        }

        // ---------------------------------------------------------------
        // Google sign-in
        // ---------------------------------------------------------------
        protected void GoogleLogin_Click(object sender, EventArgs e)
        {
            string clientId = ConfigurationManager.AppSettings["GoogleClientId"];

            if (string.IsNullOrEmpty(clientId))
            {
                ShowMessage("Google sign-in is not available right now.", false);
                System.Diagnostics.Trace.TraceError("GoogleClientId is not configured in Web.config.");
                return;
            }

            string scheme = Request.Headers["X-Forwarded-Proto"] ?? Request.Url.Scheme;
            string authority = Request.Url.Authority;
            string redirectUri = $"{scheme}://{authority}/Account/Login.aspx";

            // "state" ties the Google response to this browser session (blocks login-CSRF)
            string state = Guid.NewGuid().ToString("N");
            Session["GoogleOAuthState"] = state;

            string googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={HttpUtility.UrlEncode(clientId)}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&response_type=code&scope=email%20profile&state={state}";

            Response.Redirect(googleAuthUrl);
        }

        private void CheckGoogleAuthCallback()
        {
            string code = Request.QueryString["code"];
            if (String.IsNullOrEmpty(code)) return;

            string expectedState = Session["GoogleOAuthState"] as string;
            Session.Remove("GoogleOAuthState");
            if (string.IsNullOrEmpty(expectedState) || Request.QueryString["state"] != expectedState)
            {
                ShowMessage("Google sign-in could not be verified. Please try again.", false);
                return;
            }

            string clientId = ConfigurationManager.AppSettings["GoogleClientId"];
            string clientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
            string redirectUri = $"{Request.Url.GetLeftPart(UriPartial.Authority)}/Account/Login.aspx";

            try
            {
                string tokenJson = ExchangeCodeForToken(code, clientId, clientSecret, redirectUri);
                var tokenData = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(tokenJson);

                if (tokenData == null || !tokenData.ContainsKey("access_token")) return;

                string userInfoJson = GetGoogleUserInfo(tokenData["access_token"].ToString());
                var userInfo = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(userInfoJson);

                if (userInfo == null || !userInfo.ContainsKey("email")) return;

                // Only trust addresses Google says are verified
                if (userInfo.ContainsKey("verified_email") && userInfo["verified_email"] is bool && !(bool)userInfo["verified_email"])
                {
                    ShowMessage("Your Google email address is not verified.", false);
                    return;
                }

                string googleEmail = userInfo["email"].ToString().Trim();

                if (DoesUserExistInDatabase(googleEmail, out string userType, out string userName, out int userLevel, out int userId))
                {
                    // Same one-time-code step as password login
                    IssueOtpAndShowMfa(googleEmail, userType, userName, userLevel, userId, false);
                }
                else
                {
                    ShowMessage("The Google account (" + HttpUtility.HtmlEncode(googleEmail) + ") is not registered with The Giant Group.", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Google login failed: " + ex);
                ShowMessage("Google sign-in failed. Please try again or use your email and password.", false);
            }
        }

        private string ExchangeCodeForToken(string code, string clientId, string clientSecret, string redirectUri)
        {
            string url = "https://oauth2.googleapis.com/token";
            string postData = $"code={HttpUtility.UrlEncode(code)}&client_id={HttpUtility.UrlEncode(clientId)}&client_secret={HttpUtility.UrlEncode(clientSecret)}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&grant_type=authorization_code";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(postData);
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        private string GetGoogleUserInfo(string accessToken)
        {
            // Token goes in a header, not the URL, so it can't end up in logs
            var request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/oauth2/v2/userinfo");
            request.Method = "GET";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        // ---------------------------------------------------------------
        // Credential lookups
        // ---------------------------------------------------------------
        private bool ValidateStaffOrClientCredentials(string email, string password, out string userType, out string userName, out int userLevel, out int userId)
        {
            userType = "";
            userName = "";
            userLevel = 0;
            userId = 0;
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1. Check staff table
                string staffQuery = @"SELECT s.staffID, s.passwordHash, s.firstName, r.accessLevel 
                                     FROM staff s 
                                     LEFT JOIN Role r ON s.roleID = r.roleID 
                                     WHERE s.emailAddress = @Email";

                using (SqlCommand cmd = new SqlCommand(staffQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["passwordHash"].ToString();
                            if (SafeVerify(password, storedHash))
                            {
                                userId = Convert.ToInt32(reader["staffID"]);
                                userName = reader["firstName"].ToString();
                                userType = "Staff";
                                userLevel = reader["accessLevel"] != DBNull.Value ? Convert.ToInt32(reader["accessLevel"]) : 1;
                                return true;
                            }
                        }
                    }
                }

                // 2. Check client table
                string clientQuery = "SELECT clientID, passwordHash, clientName FROM client WHERE emailAddress = @Email";
                using (SqlCommand cmd = new SqlCommand(clientQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            object dbHash = reader["passwordHash"];
                            if (dbHash != DBNull.Value && !string.IsNullOrEmpty(dbHash.ToString()))
                            {
                                if (SafeVerify(password, dbHash.ToString()))
                                {
                                    userId = Convert.ToInt32(reader["clientID"]);
                                    userName = reader["clientName"].ToString();
                                    userType = "Client";
                                    userLevel = 0;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool DoesUserExistInDatabase(string email, out string userType, out string userName, out int userLevel, out int userId)
        {
            userType = "";
            userName = "";
            userLevel = 0;
            userId = 0;
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string staffQuery = "SELECT s.staffID, s.firstName, r.accessLevel FROM staff s LEFT JOIN Role r ON s.roleID = r.roleID WHERE s.emailAddress = @Email";
                using (SqlCommand cmd = new SqlCommand(staffQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId = Convert.ToInt32(reader["staffID"]);
                            userName = reader["firstName"].ToString();
                            userType = "Staff";
                            userLevel = reader["accessLevel"] != DBNull.Value ? Convert.ToInt32(reader["accessLevel"]) : 1;
                            return true;
                        }
                    }
                }

                string clientQuery = "SELECT clientID, clientName FROM client WHERE emailAddress = @Email";
                using (SqlCommand cmd = new SqlCommand(clientQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId = Convert.ToInt32(reader["clientID"]);
                            userName = reader["clientName"].ToString();
                            userType = "Client";
                            userLevel = 0;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private void ShowMessage(string text, bool success)
        {
            FailureText.Text = HttpUtility.HtmlEncode(text);
            alertBox.Attributes["class"] = success ? "gg-msg is-success" : "gg-msg is-error";
            alertIcon.Attributes["class"] = success ? "bi bi-check-circle-fill me-1" : "bi bi-exclamation-circle-fill me-1";
            ErrorMessage.Visible = true;
        }

        private void BackToLoginForm(string message)
        {
            MfaPanel.Visible = false;
            loginForm.Visible = true;
            ShowMessage(message, false);
        }

        private void ClearPendingSession()
        {
            string[] keys = { "PendingEmail", "PendingUserType", "PendingUserName", "PendingUserLevel", "PendingUserId",
                              "PendingRememberMe", "MfaOtpCode", "MfaExpiry", "MfaAttempts", "MfaResends", "MfaLastSent" };
            foreach (string k in keys) Session.Remove(k);
        }

        private static string GenerateOtp()
        {
            // Cryptographically secure 6-digit code (System.Random is predictable)
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[4];
                uint limit = uint.MaxValue - (uint.MaxValue % 900000);
                uint value;
                do { rng.GetBytes(bytes); value = BitConverter.ToUInt32(bytes, 0); } while (value >= limit);
                return (100000 + (value % 900000)).ToString();
            }
        }

        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static bool SafeVerify(string password, string hash)
        {
            try { return !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(password, hash); }
            catch (Exception) { return false; }   // malformed hash in the database = no match
        }

        private static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (url[0] == '/') return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
            if (url[0] == '~') return url.Length == 1 || url[1] == '/';
            return false;
        }

        private static bool IsLockedOut(string key)
        {
            int? count = HttpRuntime.Cache[key] as int?;
            return count.HasValue && count.Value >= MaxLoginFailures;
        }

        private static void RegisterFailure(string key)
        {
            int count = (HttpRuntime.Cache[key] as int?) ?? 0;
            HttpRuntime.Cache.Insert(key, count + 1, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
        }
    }
}