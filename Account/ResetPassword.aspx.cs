using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class ResetPassword : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Check if the user came through the verified gateway token
            string verified = Request.QueryString["verified"];
            if (verified == "true")
            {
                PanelResetForm.Visible = true;
                PanelAccessDenied.Visible = false;
            }
            else
            {
                PanelResetForm.Visible = false;
                PanelAccessDenied.Visible = true;
            }
        }

        protected void Reset_Click(object sender, EventArgs e)
        {
            if (IsValid)
            {
                string email = Request.QueryString["email"];
                string userType = Request.QueryString["type"];
                string newPassword = Password.Text.Trim();

                string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                int userId = 0;

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    string getIdQuery = userType == "Staff"
                        ? "SELECT staffID FROM staff WHERE emailAddress = @Email"
                        : "SELECT clientID FROM client WHERE emailAddress = @Email";

                    using (SqlCommand idCmd = new SqlCommand(getIdQuery, conn))
                    {
                        idCmd.Parameters.AddWithValue("@Email", email);
                        object res = idCmd.ExecuteScalar();
                        if (res != null) userId = Convert.ToInt32(res);
                    }

                    // Hash new password securely with BCrypt
                    string hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

                    // Update the correct table
                    string updateQuery = userType == "Staff"
                        ? "UPDATE staff SET passwordHash = @PasswordHash WHERE staffID = @ID"
                        : "UPDATE client SET passwordHash = @PasswordHash WHERE clientID = @ID";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@PasswordHash", hashedNewPassword);
                        updateCmd.Parameters.AddWithValue("@ID", userId);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                Response.Redirect("~/Account/ResetPasswordConfirmation");
            }
        }
    }
}