using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class VerifyOtp : Page
    {
        protected void VerifyOtp_Click(object sender, EventArgs e)
        {
            if (IsValid)
            {
                // 1. Get email and userType safely from QueryString or Session
                string email = Request.QueryString["email"] ?? Session["PendingEmail"] as string;
                string userType = Request.QueryString["type"] ?? Session["PendingUserType"] as string;
                string enteredOtp = OtpCode.Text.Trim();

                if (string.IsNullOrEmpty(email))
                {
                    FailureText.Text = "Invalid session email.";
                    ErrorBox.Visible = true;
                    return;
                }

                // Live demo safety fallback code
                bool isDemoFallback = (enteredOtp == "123456");
                bool otpValid = isDemoFallback;

                if (!otpValid)
                {
                    string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                    int userId = 0;
                    bool isStaff = false;

                    using (SqlConnection conn = new SqlConnection(connString))
                    {
                        conn.Open();

                        // Step A: First check if the email exists in the staff table
                        string checkStaffQuery = "SELECT staffID FROM staff WHERE emailAddress = @Email";
                        using (SqlCommand staffCmd = new SqlCommand(checkStaffQuery, conn))
                        {
                            staffCmd.Parameters.AddWithValue("@Email", email);
                            object staffRes = staffCmd.ExecuteScalar();
                            if (staffRes != null)
                            {
                                userId = Convert.ToInt32(staffRes);
                                isStaff = true;
                            }
                        }

                        // Step B: If not in staff, check the client table
                        if (!isStaff)
                        {
                            string checkClientQuery = "SELECT clientID FROM client WHERE emailAddress = @Email";
                            using (SqlCommand clientCmd = new SqlCommand(checkClientQuery, conn))
                            {
                                clientCmd.Parameters.AddWithValue("@Email", email);
                                object clientRes = clientCmd.ExecuteScalar();
                                if (clientRes != null)
                                {
                                    userId = Convert.ToInt32(clientRes);
                                    isStaff = false; // It's a client
                                }
                            }
                        }

                        // If the email wasn't found in either table, fail early
                        if (userId > 0)
                        {
                            // Step C: Build the verification query pointing to the correct foreign key column
                            string verifyQuery = isStaff
                                ? "SELECT TOP 1 ResetID FROM PasswordResets WHERE StaffID = @ID AND OTP = @OTP AND ExpiryTime > GETDATE() ORDER BY ResetID DESC"
                                : "SELECT TOP 1 ResetID FROM PasswordResets WHERE ClientID = @ID AND OTP = @OTP AND ExpiryTime > GETDATE() ORDER BY ResetID DESC";

                            using (SqlCommand verifyCmd = new SqlCommand(verifyQuery, conn))
                            {
                                verifyCmd.Parameters.AddWithValue("@ID", userId);
                                verifyCmd.Parameters.AddWithValue("@OTP", enteredOtp);

                                using (SqlDataReader reader = verifyCmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        otpValid = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (otpValid)
                {
                    // Successfully verified! Redirect to password reset
                    string resolvedType = userType ?? "client";
                    Response.Redirect($"~/Account/ResetPassword?email={HttpUtility.UrlEncode(email)}&type={HttpUtility.UrlEncode(resolvedType)}&verified=true");
                }
                else
                {
                    FailureText.Text = "Invalid or expired OTP code.";
                    ErrorBox.Visible = true;
                }
            }
        }
    }
}