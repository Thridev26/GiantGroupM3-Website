using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class ForgotPass : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void SendEmail_Click(object sender, EventArgs e)
        {
            if (IsValid)
            {
                string email = Email.Text.Trim();
                string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                bool emailExists = false;
                string userType = "";
                int userId = 0;

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // 1. Active client with this email?
                    using (SqlCommand cmd = new SqlCommand("SELECT clientID FROM client WHERE emailAddress = @Email AND status = 'Active'", conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            emailExists = true;
                            userType = "client";
                            userId = Convert.ToInt32(result);
                        }
                    }

                    // 2. Otherwise, active staff with this email?
                    if (!emailExists)
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT staffID FROM staff WHERE emailAddress = @Email AND LTRIM(RTRIM(staffStatus)) = 'Active'", conn))
                        {
                            cmd.Parameters.AddWithValue("@Email", email);
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                emailExists = true;
                                userType = "staff";
                                userId = Convert.ToInt32(result);
                            }
                        }
                    }
                }

                if (!emailExists)
                {
                    FailureText.Text = "No account found matching this email address.";
                    ErrorMessage.Visible = true;
                    return;
                }

                // GENERATE OTP, SAVE TO DATABASE, & SEND EMAIL
                try
                {
                    string otp = GenerateOtp();

                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        conn.Open();

                        // Clean up expired records older than 24 hours
                        using (SqlCommand cmdCleanup = new SqlCommand("DELETE FROM PasswordResets WHERE ExpiryTime < DATEADD(hour, -24, GETDATE());", conn))
                        {
                            cmdCleanup.ExecuteNonQuery();
                        }

                        bool isStaff = string.Equals(userType, "staff", StringComparison.OrdinalIgnoreCase);

                        string insertQuery = isStaff
                            ? "INSERT INTO PasswordResets (StaffID, OTP, ExpiryTime) VALUES (@UserId, @Otp, DATEADD(minute, 5, GETDATE()))"
                            : "INSERT INTO PasswordResets (ClientID, OTP, ExpiryTime) VALUES (@UserId, @Otp, DATEADD(minute, 5, GETDATE()))";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@Otp", otp);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Dispatch via Google Apps Script Web App (JSON is serialised, never string-built).
                    string webAppUrl = ConfigurationManager.AppSettings["EmailServiceUrl"];
                    if (string.IsNullOrEmpty(webAppUrl)) throw new InvalidOperationException("EmailServiceUrl is not configured.");
                    string jsonData = new JavaScriptSerializer().Serialize(new { email = email, otp = otp, type = "reset" });

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        client.UploadString(webAppUrl, "POST", jsonData);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Password reset code failed: " + ex);
                    FailureText.Text = "We couldn't send a reset code right now. Please try again shortly.";
                    ErrorMessage.Visible = true;
                    return;
                }

                // A fresh code starts with a fresh attempt count and no old reset grant.
                Session["ResetOtpAttempts"] = 0;
                Session.Remove("PwdResetUserId");
                Session.Remove("PwdResetIsStaff");
                Session.Remove("PwdResetExpiresUtc");

                // Redirect to your verification screen
                Response.Redirect($"~/Account/VerifyOtp?email={HttpUtility.UrlEncode(email)}&type={userType}");
            }
        }

        /// <summary>Cryptographically secure 6-digit code (System.Random is predictable).</summary>
        private static string GenerateOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[4];
                uint limit = uint.MaxValue - (uint.MaxValue % 900000);
                uint value;
                do { rng.GetBytes(bytes); value = BitConverter.ToUInt32(bytes, 0); } while (value >= limit);
                return (100000 + (value % 900000)).ToString();
            }
        }
    }
}
