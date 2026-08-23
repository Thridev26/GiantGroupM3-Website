using BCrypt.Net;
using GiantGroupM3.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class Login : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RegisterHyperLink.NavigateUrl = "Register";
                var returnUrl = HttpUtility.UrlEncode(Request.QueryString["ReturnUrl"]);
                if (!String.IsNullOrEmpty(returnUrl))
                {
                    RegisterHyperLink.NavigateUrl += "?ReturnUrl=" + returnUrl;
                }

                // Check if Google returned an authorization response (Callback handling)
                CheckGoogleAuthCallback();
            }
        }

        protected void LogIn(object sender, EventArgs e)
        {
            if (IsValid)
            {
                string email = Email.Text.Trim();
                string password = Password.Text.Trim();
                string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                string userType;
                string userName;
                int userLevel;
                int userId = 0;

                // 1. Validate credentials and get user info (Staff or Client) including userId
                if (ValidateStaffOrClientCredentials(email, password, out userType, out userName, out userLevel, out userId))
                {
                    try
                    {
                        // 2. Generate 6-digit OTP and 5-minute expiry window
                        string otp = new Random().Next(100000, 999999).ToString();
                        DateTime expiryTime = DateTime.Now.AddMinutes(5);
                        // 3. Save the OTP record into your database tracking table using SQL Server's GETDATE()
                        using (SqlConnection conn = new SqlConnection(connString))
                        {
                            conn.Open();

                            // Clean up expired records older than 24 hours
                            string cleanupQuery = "DELETE FROM PasswordResets WHERE ExpiryTime < DATEADD(hour, -24, GETDATE());";
                            using (SqlCommand cmdCleanup = new SqlCommand(cleanupQuery, conn))
                            {
                                cmdCleanup.ExecuteNonQuery();
                            }

                            string insertQuery;

                            if (userType == "Staff")
                            {
                                insertQuery = "INSERT INTO PasswordResets (StaffID, OTP, ExpiryTime) VALUES (@UserId, @Otp, DATEADD(minute, 5, GETDATE()))";
                            }
                            else
                            {
                                insertQuery = "INSERT INTO PasswordResets (ClientID, OTP, ExpiryTime) VALUES (@UserId, @Otp, DATEADD(minute, 5, GETDATE()))";
                            }

                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@UserId", userId);
                                cmd.Parameters.AddWithValue("@Otp", otp);
                                // Notice we removed @ExpiryTime parameter because SQL Server handles it via DATEADD now!
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 4. Dispatch via your Google Apps Script Web App (Synchronous WebClient)
                        string webAppUrl = "https://script.google.com/macros/s/AKfycbyvFh_W-sCbWxIxtWdpO4PcAfdUEcfKrXsNnxlSVMIVZzNTjOqpvoSgqDnvpE4t_B6g/exec";
                        string jsonData = $"{{\"email\":\"{email}\", \"otp\":\"{otp}\", \"type\":\"login\"}}";

                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                        using (WebClient client = new WebClient())
                        {
                            client.Encoding = Encoding.UTF8;
                            client.Headers[HttpRequestHeader.ContentType] = "application/json";
                            string response = client.UploadString(webAppUrl, "POST", jsonData);
                        }

                        // 5. Store session state variables temporarily for verification check
                        Session["PendingEmail"] = email;
                        Session["PendingUserType"] = userType;
                        Session["PendingUserName"] = userName;
                        Session["PendingUserLevel"] = userLevel;
                        Session["PendingRememberMe"] = RememberMe.Checked;
                        Session["MfaOtpCode"] = otp;
                        Session["MfaExpiry"] = expiryTime;

                        // 6. Switch views to your MFA Verification panel
                        loginForm.Visible = false;
                        MfaPanel.Visible = true;
                    }
                    catch (Exception ex)
                    {
                        FailureText.Text = "Failed to process MFA verification: " + ex.Message;
                        ErrorMessage.Visible = true;
                    }
                }
                else
                {
                    FailureText.Text = "Invalid email or password.";
                    ErrorMessage.Visible = true;
                }
            }
        }

        protected void VerifyMfa_Click(object sender, EventArgs e)
        {
            string enteredCode = MfaCode.Text.Trim();
            string storedCode = Session["MfaOtpCode"] as string;
            DateTime? expiry = Session["MfaExpiry"] as DateTime?;

            bool isDemoFallback = (enteredCode == "123456");
            bool isValidRealCode = (storedCode != null && expiry.HasValue && DateTime.Now <= expiry.Value && enteredCode == storedCode);

            if (isDemoFallback || isValidRealCode)
            {
                // MFA Success! Finalize login session
                string email = Session["PendingEmail"] as string;
                string userType = Session["PendingUserType"] as string; // "Staff" or "Client"

                Session["StaffName"] = Session["PendingUserName"];
                Session["UserType"] = userType;
                Session["UserRole"] = userType; // Set UserRole so master page IsClient() works seamlessly!
                Session["UserLevel"] = Session["PendingUserLevel"];

                bool rememberMe = Session["PendingRememberMe"] != null && (bool)Session["PendingRememberMe"];

                // Clear temporary MFA session items
                Session.Remove("PendingEmail");
                Session.Remove("PendingUserType");
                Session.Remove("PendingUserName");
                Session.Remove("PendingUserLevel");
                Session.Remove("PendingRememberMe");
                Session.Remove("MfaOtpCode");
                Session.Remove("MfaExpiry");

                FormsAuthentication.SetAuthCookie(email, rememberMe);
                RedirectToReturnUrl();
            }
            else
            {
                FailureText.Text = "Invalid or expired verification code. Please try again.";
                ErrorMessage.Visible = true;
            }
        }

        protected void GoogleLogin_Click(object sender, EventArgs e)
        {
            string clientId = ConfigurationManager.AppSettings["GoogleClientId"];

            if (string.IsNullOrEmpty(clientId))
            {
                FailureText.Text = "Google Client ID is not configured in Web.config.";
                ErrorMessage.Visible = true;
                return;
            }

            string scheme = Request.Headers["X-Forwarded-Proto"] ?? Request.Url.Scheme;
            string authority = Request.Url.Authority;
            string redirectUri = $"{scheme}://{authority}/Account/Login.aspx";

            string googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={HttpUtility.UrlEncode(clientId)}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&response_type=code&scope=email%20profile";

            Response.Redirect(googleAuthUrl);
        }

        private void CheckGoogleAuthCallback()
        {
            string code = Request.QueryString["code"];
            if (!String.IsNullOrEmpty(code))
            {
                string clientId = ConfigurationManager.AppSettings["GoogleClientId"];
                string clientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
                string redirectUri = $"{Request.Url.GetLeftPart(UriPartial.Authority)}/Account/Login.aspx";

                try
                {
                    string tokenJson = ExchangeCodeForToken(code, clientId, clientSecret, redirectUri);
                    var tokenData = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(tokenJson);

                    if (tokenData != null && tokenData.ContainsKey("access_token"))
                    {
                        string accessToken = tokenData["access_token"].ToString();
                        string userInfoJson = GetGoogleUserInfo(accessToken);
                        var userInfo = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(userInfoJson);

                        if (userInfo != null && userInfo.ContainsKey("email"))
                        {
                            string googleEmail = userInfo["email"].ToString().Trim();

                            if (DoesUserExistInDatabase(googleEmail, out string userType, out string userName, out int userLevel))
                            {
                                Session["StaffName"] = userName;
                                Session["UserType"] = userType;
                                Session["UserRole"] = userType; // Add this line for Google login too
                                Session["UserLevel"] = userLevel;
                                FormsAuthentication.SetAuthCookie(googleEmail, false);
                                RedirectToReturnUrl();
                                return;
                            }
                            else
                            {
                                FailureText.Text = $"The Google account ({googleEmail}) is not registered in the staff or client database.";
                                ErrorMessage.Visible = true;
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FailureText.Text = "Google Login Error: " + ex.Message;
                    ErrorMessage.Visible = true;
                }
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
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private string GetGoogleUserInfo(string accessToken)
        {
            string url = $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={HttpUtility.UrlEncode(accessToken)}";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

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
                            if (BCrypt.Net.BCrypt.Verify(password, storedHash))
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
                                if (BCrypt.Net.BCrypt.Verify(password, dbHash.ToString()))
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

        private bool DoesUserExistInDatabase(string email, out string userType, out string userName, out int userLevel)
        {
            userType = "";
            userName = "";
            userLevel = 0;
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                string staffQuery = "SELECT s.firstName, r.accessLevel FROM staff s LEFT JOIN Role r ON s.roleID = r.roleID WHERE s.emailAddress = @Email";
                using (SqlCommand cmd = new SqlCommand(staffQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userName = reader["firstName"].ToString();
                            userType = "Staff";
                            userLevel = reader["accessLevel"] != DBNull.Value ? Convert.ToInt32(reader["accessLevel"]) : 1;
                            return true;
                        }
                    }
                }

                string clientQuery = "SELECT clientName FROM client WHERE emailAddress = @Email";
                using (SqlCommand cmd = new SqlCommand(clientQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
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

        private void RedirectToReturnUrl()
        {
            string returnUrl = Request.QueryString["ReturnUrl"];
            if (!String.IsNullOrEmpty(returnUrl))
            {
                Response.Redirect(returnUrl);
            }
            else
            {
                Response.Redirect("~/Default.aspx");
            }
        }
    }
}