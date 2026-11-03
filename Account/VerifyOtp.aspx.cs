using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class VerifyOtp : Page
    {
        private const int MaxResetOtpAttempts = 5;
        private const int ResetGrantMinutes = 10;

        protected void VerifyOtp_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;

            string email = (Request.QueryString["email"] ?? (Session["PendingEmail"] as string) ?? string.Empty).Trim();
            string enteredOtp = (OtpCode.Text ?? string.Empty).Trim();

            if (email.Length == 0)
            {
                ShowError("Invalid session email.");
                return;
            }

            int attempts = (Session["ResetOtpAttempts"] as int?) ?? 0;
            if (attempts >= MaxResetOtpAttempts)
            {
                ShowError("Too many incorrect codes. Please request a new code and try again.");
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            bool isStaff;
            int userId;
            if (!FindAccount(connString, email, out isStaff, out userId))
            {
                ShowError("Invalid or expired OTP code.");
                return;
            }

            // The fallback code is only honoured when AllowDemoMfaCode is "true" in Web.config.
            bool demoEnabled = string.Equals(ConfigurationManager.AppSettings["AllowDemoMfaCode"], "true", StringComparison.OrdinalIgnoreCase);
            bool isDemoFallback = demoEnabled && enteredOtp == "123456";

            bool otpValid = isDemoFallback || IsOtpValid(connString, userId, isStaff, enteredOtp);
            if (!otpValid)
            {
                Session["ResetOtpAttempts"] = attempts + 1;
                ShowError("Invalid or expired OTP code.");
                return;
            }

            // The code is used up once it has been accepted.
            ConsumeResetCodes(connString, userId, isStaff);

            // Server-side grant: ResetPassword only trusts this, never a URL parameter.
            Session.Remove("ResetOtpAttempts");
            Session["PwdResetUserId"] = userId;
            Session["PwdResetIsStaff"] = isStaff;
            Session["PwdResetExpiresUtc"] = DateTime.UtcNow.AddMinutes(ResetGrantMinutes);

            Response.Redirect("~/Account/ResetPassword?verified=true");
        }

        private void ShowError(string message)
        {
            FailureText.Text = HttpUtility.HtmlEncode(message);
            ErrorBox.Visible = true;
        }

        /// <summary>Finds an active staff member first, then an active client, for this email.</summary>
        private static bool FindAccount(string connString, string email, out bool isStaff, out int userId)
        {
            isStaff = false;
            userId = 0;
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlCommand staffCmd = new SqlCommand("SELECT staffID FROM staff WHERE emailAddress = @Email AND LTRIM(RTRIM(staffStatus)) = 'Active'", conn))
                {
                    staffCmd.Parameters.AddWithValue("@Email", email);
                    object staffRes = staffCmd.ExecuteScalar();
                    if (staffRes != null)
                    {
                        userId = Convert.ToInt32(staffRes);
                        isStaff = true;
                        return true;
                    }
                }
                using (SqlCommand clientCmd = new SqlCommand("SELECT clientID FROM client WHERE emailAddress = @Email AND status = 'Active'", conn))
                {
                    clientCmd.Parameters.AddWithValue("@Email", email);
                    object clientRes = clientCmd.ExecuteScalar();
                    if (clientRes != null)
                    {
                        userId = Convert.ToInt32(clientRes);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsOtpValid(string connString, int userId, bool isStaff, string otp)
        {
            string sql = isStaff
                ? "SELECT TOP 1 ResetID FROM PasswordResets WHERE StaffID = @ID AND OTP = @OTP AND ExpiryTime > GETDATE() ORDER BY ResetID DESC"
                : "SELECT TOP 1 ResetID FROM PasswordResets WHERE ClientID = @ID AND OTP = @OTP AND ExpiryTime > GETDATE() ORDER BY ResetID DESC";
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                cmd.Parameters.AddWithValue("@OTP", otp);
                conn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        private static void ConsumeResetCodes(string connString, int userId, bool isStaff)
        {
            string sql = isStaff
                ? "DELETE FROM PasswordResets WHERE StaffID = @ID"
                : "DELETE FROM PasswordResets WHERE ClientID = @ID";
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
