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
                string email = Request.QueryString["email"];
                string userType = Request.QueryString["type"];
                string enteredOtp = OtpCode.Text.Trim();

                if (string.IsNullOrEmpty(email))
                {
                    FailureText.Text = "Invalid session.";
                    ErrorBox.Visible = true;
                    return;
                }

                string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                bool otpValid = false;
                int userId = 0;

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // Get user ID
                    string getIdQuery = userType == "staff"
                        ? "SELECT staffID FROM staff WHERE emailAddress = @Email"
                        : "SELECT clientID FROM client WHERE emailAddress = @Email";

                    using (SqlCommand idCmd = new SqlCommand(getIdQuery, conn))
                    {
                        idCmd.Parameters.AddWithValue("@Email", email);
                        object res = idCmd.ExecuteScalar();
                        if (res != null) userId = Convert.ToInt32(res);
                    }

                    // Check PasswordResets table
                    string verifyQuery = "SELECT TOP 1 ResetID FROM PasswordResets WHERE StaffID = @ID AND OTP = @OTP AND ExpiryTime > GETDATE() ORDER BY ResetID DESC";
                    using (SqlCommand verifyCmd = new SqlCommand(verifyQuery, conn))
                    {
                        verifyCmd.Parameters.AddWithValue("@ID", userId);
                        verifyCmd.Parameters.AddWithValue("@OTP", enteredOtp);

                        using (SqlDataReader reader = verifyCmd.ExecuteReader())
                        {
                            if (reader.HasRows) otpValid = true;
                        }
                    }
                }

                if (otpValid)
                {
                    // OTP verified successfully! NOW redirect to the Reset Password page
                    Response.Redirect($"~/Account/ResetPassword?email={HttpUtility.UrlEncode(email)}&type={userType}&verified=true");
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