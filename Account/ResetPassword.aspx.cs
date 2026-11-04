using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class ResetPassword : Page
    {
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 72;

        protected void Page_Load(object sender, EventArgs e)
        {
            bool granted = HasValidGrant();
            PanelResetForm.Visible = granted;
            PanelAccessDenied.Visible = !granted;
        }

        protected void Reset_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;

            // Only a code verified on the previous step (held server-side) allows a reset.
            if (!HasValidGrant())
            {
                ClearGrant();
                PanelResetForm.Visible = false;
                PanelAccessDenied.Visible = true;
                return;
            }

            string newPassword = (Password.Text ?? string.Empty).Trim();
            string problem = ValidatePassword(newPassword);
            if (problem != null)
            {
                lblPasswordPolicy.Text = problem;
                lblPasswordPolicy.Visible = true;
                return;
            }

            int userId = (int)Session["PwdResetUserId"];
            bool isStaff = (bool)Session["PwdResetIsStaff"];
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            string updateQuery = isStaff
                ? "UPDATE staff SET passwordHash = @PasswordHash WHERE staffID = @ID AND LTRIM(RTRIM(staffStatus)) = 'Active'"
                : "UPDATE client SET passwordHash = @PasswordHash WHERE clientID = @ID AND status = 'Active'";

            int rows;
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
            {
                updateCmd.Parameters.AddWithValue("@PasswordHash", hashedNewPassword);
                updateCmd.Parameters.AddWithValue("@ID", userId);
                conn.Open();
                rows = updateCmd.ExecuteNonQuery();
            }

            ClearGrant();

            if (rows != 1)
            {
                PanelResetForm.Visible = false;
                PanelAccessDenied.Visible = true;
                return;
            }

            Response.Redirect("~/Account/ResetPasswordConfirmation");
        }

        private bool HasValidGrant()
        {
            return Session["PwdResetUserId"] is int
                && Session["PwdResetIsStaff"] is bool
                && Session["PwdResetExpiresUtc"] is DateTime
                && DateTime.UtcNow <= (DateTime)Session["PwdResetExpiresUtc"];
        }

        private void ClearGrant()
        {
            Session.Remove("PwdResetUserId");
            Session.Remove("PwdResetIsStaff");
            Session.Remove("PwdResetExpiresUtc");
        }

        /// <summary>Same rules as account registration, so every password in the system meets one standard.</summary>
        private static string ValidatePassword(string password)
        {
            if (password.Length < MinPasswordLength) return "Your password must be at least 8 characters long.";
            if (password.Length > MaxPasswordLength) return "Your password can be at most 72 characters long.";
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")
                || !System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")
                || !System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]"))
                return "Your password needs an uppercase letter, a lowercase letter and a number.";
            return null;
        }
    }
}
