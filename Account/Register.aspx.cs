using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class Register : Page
    {
        protected void CreateUser_Click(object sender, EventArgs e)
        {
            if (!IsValid) return;

            // Honeypot: bots fill hidden fields, people don't. Pretend it worked and do nothing.
            if (!string.IsNullOrEmpty(Request.Form["website"]))
            {
                Response.Redirect("~/Account/Login?registered=true");
                return;
            }

            string email = Email.Text.Trim();
            string password = Password.Text.Trim();
            string confirm = ConfirmPassword.Text.Trim();
            string firstName = FirstName.Text.Trim();
            string surname = Surname.Text.Trim();
            string phone = PhoneNumber.Text.Trim();
            string clientType = ClientType.SelectedValue.Trim(); // Residential or Commercial

            // ---- Server-side validation (the browser checks can be bypassed) ----
            string problem = Validate(firstName, surname, email, phone, clientType, password, confirm);
            if (problem != null)
            {
                ShowError(problem);
                return;
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // 1. Staff members can't register as clients
                    using (SqlCommand staffCmd = new SqlCommand("SELECT COUNT(*) FROM staff WHERE emailAddress = @Email", conn))
                    {
                        staffCmd.Parameters.AddWithValue("@Email", email);
                        if ((int)staffCmd.ExecuteScalar() > 0)
                        {
                            ShowError("Staff members cannot register through the portal. Please use the standard login page.");
                            return;
                        }
                    }

                    // 2. Does this email exist in the client table, and do they already have a password?
                    bool clientExists = false;
                    bool hasPasswordAlready = false;

                    using (SqlCommand clientCmd = new SqlCommand("SELECT clientID, passwordHash FROM client WHERE emailAddress = @Email", conn))
                    {
                        clientCmd.Parameters.AddWithValue("@Email", email);
                        using (SqlDataReader reader = clientCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                clientExists = true;
                                object dbHash = reader["passwordHash"];
                                hasPasswordAlready = dbHash != DBNull.Value && !string.IsNullOrEmpty(dbHash.ToString());
                            }
                        }
                    }

                    // 3. Handle the three scenarios
                    if (hasPasswordAlready)
                    {
                        ShowError("An account with this email already exists and is active. Please log in.");
                        return;
                    }
                    else if (clientExists)
                    {
                        // Client was pre-created by admin; they are now claiming the account.
                        // (Login still requires an emailed one-time code, so the password alone is not enough to get in.)
                        string updateQuery = @"UPDATE client 
                                             SET passwordHash = @PasswordHash, 
                                                 clientName = ISNULL(NULLIF(@FirstName, ''), clientName), 
                                                 clientSurname = ISNULL(NULLIF(@Surname, ''), clientSurname),
                                                 phoneNumber = ISNULL(NULLIF(@Phone, ''), phoneNumber),
                                                 clientType = @ClientType
                                             WHERE emailAddress = @Email";

                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            updateCmd.Parameters.AddWithValue("@FirstName", firstName);
                            updateCmd.Parameters.AddWithValue("@Surname", surname);
                            updateCmd.Parameters.AddWithValue("@Phone", phone);
                            updateCmd.Parameters.AddWithValue("@ClientType", clientType);
                            updateCmd.Parameters.AddWithValue("@Email", email);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Brand new client registration
                        string insertQuery = @"INSERT INTO client (clientName, clientSurname, clientType, emailAddress, status, dateAdded, phoneNumber, passwordHash) 
                                             VALUES (@FirstName, @Surname, @ClientType, @Email, 'Active', GETDATE(), @Phone, @PasswordHash)";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@FirstName", firstName);
                            insertCmd.Parameters.AddWithValue("@Surname", surname);
                            insertCmd.Parameters.AddWithValue("@ClientType", clientType);
                            insertCmd.Parameters.AddWithValue("@Email", email);
                            insertCmd.Parameters.AddWithValue("@Phone", phone);
                            insertCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Registration failed: " + ex);
                ShowError("We couldn't create your account right now. Please try again in a moment.");
                return;
            }

            // Successfully processed, redirect to Login with a success flag
            Response.Redirect("~/Account/Login?registered=true");
        }

        private static string Validate(string first, string last, string email, string phone, string clientType, string password, string confirm)
        {
            var namePattern = new Regex(@"^[\p{L}][\p{L}\s'\-\.]{0,59}$");
            if (!namePattern.IsMatch(first)) return "Please enter a valid first name (letters only).";
            if (!namePattern.IsMatch(last)) return "Please enter a valid surname (letters only).";

            try
            {
                var addr = new MailAddress(email);
                if (!string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase) || email.Length > 254)
                    return "Please enter a valid email address.";
            }
            catch (FormatException) { return "Please enter a valid email address."; }

            if (clientType != "Residential" && clientType != "Commercial") return "Please choose a client type.";

            if (!string.IsNullOrEmpty(phone))
            {
                string digits = Regex.Replace(phone, @"\D", "");
                if (!Regex.IsMatch(phone, @"^[0-9+\-\s()]{7,20}$") || digits.Length < 7 || digits.Length > 15)
                    return "Please enter a valid phone number, for example 0821234567.";
            }

            if (password.Length < 8) return "Your password must be at least 8 characters long.";
            if (password.Length > 72) return "Your password can be at most 72 characters long.";
            if (!Regex.IsMatch(password, "[A-Z]") || !Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[0-9]"))
                return "Your password needs an uppercase letter, a lowercase letter and a number.";
            if (!string.Equals(password, confirm, StringComparison.Ordinal)) return "The passwords you entered do not match.";

            return null;
        }

        private void ShowError(string message)
        {
            FailureText.Text = HttpUtility.HtmlEncode(message);
            ErrorPlaceHolder.Visible = true;
            // Names, email, phone and client type are kept so the user doesn't have to retype them.
            // Password boxes are cleared automatically by ASP.NET on postback.
        }
    }
}