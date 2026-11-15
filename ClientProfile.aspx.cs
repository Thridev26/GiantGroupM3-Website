using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ClientProfile : Page
    {
        private readonly string _connString =
            ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

        private int CurrentClientID
        {
            get
            {
                int id;
                return int.TryParse(Session["ClientID"]?.ToString(), out id) ? id : 0;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!Request.IsAuthenticated || CurrentClientID <= 0)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (string.IsNullOrWhiteSpace(_connString))
                throw new ConfigurationErrorsException("The DefaultConnection connection string is missing from Web.config.");

            if (!IsPostBack)
                LoadProfile();
        }

        private void LoadProfile()
        {
            const string sql = @"
                SELECT clientName, clientSurname, emailAddress, phoneNumber
                FROM dbo.Client
                WHERE clientID = @clientID AND status = 'Active';";

            using (var conn = new SqlConnection(_connString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = CurrentClientID;
                conn.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!reader.Read())
                    {
                        ShowMessage("We couldn't find an active client profile for this account. Please contact support.", true);
                        btnSave.Enabled = false;
                        return;
                    }

                    txtFirstName.Text = Convert.ToString(reader["clientName"]);
                    txtSurname.Text = Convert.ToString(reader["clientSurname"]);
                    txtEmail.Text = Convert.ToString(reader["emailAddress"]);
                    txtPhone.Text = Convert.ToString(reader["phoneNumber"]);
                }
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Page.Validate("Profile");
            if (!Page.IsValid)
                return;

            string firstName = txtFirstName.Text.Trim();
            string surname = txtSurname.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();

            if (firstName.Length > 40 || surname.Length > 40 || email.Length > 50 || phone.Length > 15)
            {
                ShowMessage("One or more fields exceed the maximum permitted length.", true);
                return;
            }

            const string sql = @"
                UPDATE dbo.Client
                SET clientName = @clientName,
                    clientSurname = @clientSurname,
                    emailAddress = @emailAddress,
                    phoneNumber = @phoneNumber
                WHERE clientID = @clientID AND status = 'Active';";

            try
            {
                using (var conn = new SqlConnection(_connString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@clientName", SqlDbType.VarChar, 40).Value = firstName;
                    cmd.Parameters.Add("@clientSurname", SqlDbType.VarChar, 40).Value = surname;
                    cmd.Parameters.Add("@emailAddress", SqlDbType.VarChar, 50).Value = email;
                    cmd.Parameters.Add("@phoneNumber", SqlDbType.VarChar, 15).Value = phone;
                    cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = CurrentClientID;

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 1)
                    {
                        ShowMessage("Your profile has been updated successfully.", false);
                        // Keep the portal greeting in sync if the session stores the client's name.
                        Session["Username"] = firstName;
                    }
                    else
                    {
                        ShowMessage("Your profile could not be updated. Please contact support.", true);
                    }
                }
            }
            catch (SqlException ex)
            {
                // SQL Server error 2601/2627 means the unique email address is already in use.
                if (ex.Number == 2601 || ex.Number == 2627)
                    ShowMessage("That email address is already associated with another client account. Please use a different email.", true);
                else
                {
                    System.Diagnostics.Trace.TraceError("ClientProfile update failed: " + ex);
                    ShowMessage("We couldn't save your changes right now. Please try again later.", true);
                }
            }
        }

        // ------------------------------------------------------------------
        //  Change password
        // ------------------------------------------------------------------
        private const int MaxPasswordFailures = 5;   // wrong current password, per client, per 15 minutes

        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            Page.Validate("Password");
            if (!Page.IsValid)
                return;

            // Same trimming as the login and registration pages, so the saved password can always be typed back in.
            string current = txtCurrentPassword.Text.Trim();
            string newPassword = txtNewPassword.Text.Trim();
            string confirm = txtConfirmPassword.Text.Trim();

            // Brute-force protection: someone on an unattended signed-in session must not be able to guess the current password.
            string throttleKey = "pwchange:" + CurrentClientID;
            if (IsLockedOut(throttleKey))
            {
                ShowPasswordMessage("Too many unsuccessful attempts. Please wait 15 minutes and try again.", true);
                return;
            }

            string problem = ValidateNewPassword(newPassword, confirm);
            if (problem != null)
            {
                ShowPasswordMessage(problem, true);
                return;
            }

            try
            {
                string storedHash;
                string email;

                const string selectSql = @"
                    SELECT passwordHash, emailAddress
                    FROM dbo.Client
                    WHERE clientID = @clientID AND status = 'Active';";

                using (var conn = new SqlConnection(_connString))
                using (var cmd = new SqlCommand(selectSql, conn))
                {
                    cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = CurrentClientID;
                    conn.Open();
                    using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (!reader.Read())
                        {
                            ShowPasswordMessage("We couldn't find an active client profile for this account. Please contact support.", true);
                            return;
                        }

                        storedHash = reader["passwordHash"] == DBNull.Value ? null : Convert.ToString(reader["passwordHash"]);
                        email = Convert.ToString(reader["emailAddress"]);
                    }
                }

                // Accounts created through Google sign-in have no password to change.
                if (string.IsNullOrEmpty(storedHash))
                {
                    ShowPasswordMessage("This account does not have a password yet. Use \"Forgot password\" on the sign-in page to set one.", true);
                    return;
                }

                if (!SafeVerify(current, storedHash))
                {
                    RegisterFailure(throttleKey);
                    ShowPasswordMessage("Your current password is incorrect.", true);
                    return;
                }

                // The current password is right, so the attempt counter starts again.
                HttpRuntime.Cache.Remove(throttleKey);

                if (SafeVerify(newPassword, storedHash))
                {
                    ShowPasswordMessage("Your new password must be different from your current password.", true);
                    return;
                }

                if (!string.IsNullOrEmpty(email) && string.Equals(newPassword, email, StringComparison.OrdinalIgnoreCase))
                {
                    ShowPasswordMessage("Your password cannot be the same as your email address.", true);
                    return;
                }

                string newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // Only update if the password is still the one we just verified (guards against a change made at the same moment elsewhere).
                const string updateSql = @"
                    UPDATE dbo.Client
                    SET passwordHash = @newHash
                    WHERE clientID = @clientID AND status = 'Active' AND passwordHash = @oldHash;";

                using (var conn = new SqlConnection(_connString))
                using (var cmd = new SqlCommand(updateSql, conn))
                {
                    cmd.Parameters.Add("@newHash", SqlDbType.VarChar, 255).Value = newHash;
                    cmd.Parameters.Add("@oldHash", SqlDbType.VarChar, 255).Value = storedHash;
                    cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = CurrentClientID;

                    conn.Open();
                    if (cmd.ExecuteNonQuery() == 1)
                        ShowPasswordMessage("Your password has been updated. Use your new password the next time you sign in.", false);
                    else
                        ShowPasswordMessage("Your password could not be updated because it was changed elsewhere. Please try again.", true);
                }
            }
            catch (Exception ex)
            {
                // Never log the passwords themselves.
                System.Diagnostics.Trace.TraceError("ClientProfile password change failed: " + ex);
                ShowPasswordMessage("We couldn't update your password right now. Please try again later.", true);
            }
        }

        // Same rules as the registration page.
        private static string ValidateNewPassword(string password, string confirm)
        {
            if (password.Length < 8) return "Your password must be at least 8 characters long.";
            if (password.Length > 72) return "Your password can be at most 72 characters long.";
            if (!Regex.IsMatch(password, "[A-Z]") || !Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[0-9]"))
                return "Your password needs an uppercase letter, a lowercase letter and a number.";
            if (!string.Equals(password, confirm, StringComparison.Ordinal)) return "The new passwords do not match.";
            return null;
        }

        private static bool SafeVerify(string password, string hash)
        {
            try { return !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(password, hash); }
            catch (Exception) { return false; }   // malformed hash in the database = no match
        }

        private static bool IsLockedOut(string key)
        {
            int? count = HttpRuntime.Cache[key] as int?;
            return count.HasValue && count.Value >= MaxPasswordFailures;
        }

        private static void RegisterFailure(string key)
        {
            int count = (HttpRuntime.Cache[key] as int?) ?? 0;
            HttpRuntime.Cache.Insert(key, count + 1, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
        }

        private void ShowPasswordMessage(string message, bool isError)
        {
            litPasswordMessage.Text = HttpUtility.HtmlEncode(message);
            pnlPasswordMessage.Visible = true;
            pnlPasswordMessage.CssClass = isError ? "gg-msg is-error mt-4 mb-3" : "gg-msg is-success mt-4 mb-3";
        }

        private void ShowMessage(string message, bool isError)
        {
            litMessage.Text = HttpUtility.HtmlEncode(message);
            pnlMessage.Visible = true;
            pnlMessage.CssClass = isError ? "gg-alert mb-3" : "gg-alert mb-3";
        }
    }
}
