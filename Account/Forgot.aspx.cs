using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Web;
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

                    // 1. Check if email exists in client table and get ClientID
                    string clientQuery = "SELECT ClientID FROM client WHERE emailAddress = @Email";
                    using (SqlCommand cmd = new SqlCommand(clientQuery, conn))
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

                    // 2. If not a client, check staff table and get StaffID
                    if (!emailExists)
                    {
                        string staffQuery = "SELECT StaffID FROM staff WHERE emailAddress = @Email";
                        using (SqlCommand cmd = new SqlCommand(staffQuery, conn))
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

                // ==========================================
                // GENERATE OTP, SAVE TO DATABASE, & SEND EMAIL
                // ==========================================
                try
                {
                    string otp = new Random().Next(100000, 999999).ToString();
                    DateTime expiryTime = DateTime.Now.AddMinutes(5); // 5-minute window matching your table schema

                    // Save the OTP record into your PasswordResets table
                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        conn.Open();
                        // 1. Clean up expired tokens older than 24 hours
                        string cleanupQuery = "DELETE FROM PasswordResets WHERE ExpiryTime < DATEADD(hour, -24, GETDATE());";
                        using (SqlCommand cmdCleanup = new SqlCommand(cleanupQuery, conn))
                        {
                            cmdCleanup.ExecuteNonQuery();
                        }

                        string insertQuery = "INSERT INTO PasswordResets (StaffID, OTP, ExpiryTime) VALUES (@UserId, @Otp, @ExpiryTime)";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@Otp", otp);
                            cmd.Parameters.AddWithValue("@ExpiryTime", expiryTime);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Dispatch via Google Apps Script Web App
                    string webAppUrl = "https://script.google.com/macros/s/AKfycbyvFh_W-sCbWxIxtWdpO4PcAfdUEcfKrXsNnxlSVMIVZzNTjOqpvoSgqDnvpE4t_B6g/exec";
                    string jsonData = $"{{\"email\":\"{email}\", \"otp\":\"{otp}\", \"type\":\"reset\"}}";

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                    using (WebClient client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers[HttpRequestHeader.ContentType] = "application/json";
                        string response = client.UploadString(webAppUrl, "POST", jsonData);
                    }
                }
                catch (Exception ex)
                {
                    FailureText.Text = "Failed to process password reset: " + ex.Message;
                    ErrorMessage.Visible = true;
                    return;
                }

                // Redirect to your verification screen
                Response.Redirect($"~/Account/VerifyOtp?email={HttpUtility.UrlEncode(email)}&type={userType}");
            }
        }
    }
}