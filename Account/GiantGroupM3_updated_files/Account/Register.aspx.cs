using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class Register : Page
    {
        private const int ClaimOtpMinutes = 5;
        private const int MaxClaimOtpAttempts = 5;

        protected void Page_Load(object sender, EventArgs e)
        {
            bool claimPending = Session["ClaimEmail"] != null;
            pnlRegister.Visible = !claimPending;
            pnlClaimVerify.Visible = claimPending;
            if (claimPending)
                litClaimEmail.Text = HttpUtility.HtmlEncode("We sent a 6-digit code to " + (string)Session["ClaimEmail"] + ".");
        }

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
                        // A pre-created client is claiming the account. Prove they control the email
                        // address (emailed code) before any password is accepted.
                        BeginClaimVerification(email, firstName, surname, phone, clientType, hashedPassword);
                        return;
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

        // ---------------------------------------------------------------
        // Claiming a pre-created account: emailed code first
        // ---------------------------------------------------------------
        private void BeginClaimVerification(string email, string firstName, string surname, string phone, string clientType, string hashedPassword)
        {
            string otp = GenerateOtp();

            try
            {
                SendVerificationCode(email, otp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Claim verification code failed: " + ex);
                ShowError("We couldn't send a verification code right now. Please try again in a moment.");
                return;
            }

            Session["ClaimEmail"] = email;
            Session["ClaimFirstName"] = firstName;
            Session["ClaimSurname"] = surname;
            Session["ClaimPhone"] = phone;
            Session["ClaimClientType"] = clientType;
            Session["ClaimPasswordHash"] = hashedPassword;
            Session["ClaimOtp"] = otp;
            Session["ClaimOtpExpiry"] = DateTime.UtcNow.AddMinutes(ClaimOtpMinutes);
            Session["ClaimAttempts"] = 0;

            pnlRegister.Visible = false;
            pnlClaimVerify.Visible = true;
            litClaimEmail.Text = HttpUtility.HtmlEncode("We sent a 6-digit code to " + email + ".");
            litClaimMessage.Text = string.Empty;
        }

        protected void ClaimVerify_Click(object sender, EventArgs e)
        {
            string email = Session["ClaimEmail"] as string;
            string storedOtp = Session["ClaimOtp"] as string;

            if (email == null || storedOtp == null || !(Session["ClaimOtpExpiry"] is DateTime))
            {
                ClearClaim();
                ShowRegistrationForm();
                ShowError("Your verification session has expired. Please fill in the form again.");
                return;
            }

            DateTime expiry = (DateTime)Session["ClaimOtpExpiry"];
            if (DateTime.UtcNow > expiry)
            {
                ClearClaim();
                ShowRegistrationForm();
                ShowError("That code has expired. Please fill in the form again to get a new one.");
                return;
            }

            int attempts = (Session["ClaimAttempts"] as int?) ?? 0;
            if (attempts >= MaxClaimOtpAttempts)
            {
                ClearClaim();
                ShowRegistrationForm();
                ShowError("Too many incorrect codes. Please fill in the form again.");
                return;
            }

            string entered = (ClaimOtpCode.Text ?? string.Empty).Trim();

            // Same fallback rule as login: only honoured when AllowDemoMfaCode is "true" in Web.config.
            bool demoEnabled = string.Equals(ConfigurationManager.AppSettings["AllowDemoMfaCode"], "true", StringComparison.OrdinalIgnoreCase);
            bool valid = (demoEnabled && entered == "123456") || ConstantTimeEquals(entered, storedOtp);

            if (!valid)
            {
                attempts++;
                Session["ClaimAttempts"] = attempts;
                int left = MaxClaimOtpAttempts - attempts;
                if (left <= 0)
                {
                    ClearClaim();
                    ShowRegistrationForm();
                    ShowError("Too many incorrect codes. Please fill in the form again.");
                    return;
                }
                litClaimMessage.Text = HttpUtility.HtmlEncode("Invalid or expired code. You have " + left + " attempt" + (left == 1 ? "" : "s") + " left.");
                return;
            }

            // Code accepted: set the password, but only if the account still has none.
            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            int rows;
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                using (SqlCommand updateCmd = new SqlCommand(@"UPDATE client
                                             SET passwordHash = @PasswordHash,
                                                 clientName = ISNULL(NULLIF(@FirstName, ''), clientName),
                                                 clientSurname = ISNULL(NULLIF(@Surname, ''), clientSurname),
                                                 phoneNumber = ISNULL(NULLIF(@Phone, ''), phoneNumber),
                                                 clientType = @ClientType
                                             WHERE emailAddress = @Email
                                               AND (passwordHash IS NULL OR passwordHash = '')", conn))
                {
                    updateCmd.Parameters.AddWithValue("@PasswordHash", (string)Session["ClaimPasswordHash"]);
                    updateCmd.Parameters.AddWithValue("@FirstName", (string)Session["ClaimFirstName"]);
                    updateCmd.Parameters.AddWithValue("@Surname", (string)Session["ClaimSurname"]);
                    updateCmd.Parameters.AddWithValue("@Phone", (string)Session["ClaimPhone"]);
                    updateCmd.Parameters.AddWithValue("@ClientType", (string)Session["ClaimClientType"]);
                    updateCmd.Parameters.AddWithValue("@Email", email);
                    conn.Open();
                    rows = updateCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Account claim failed: " + ex);
                ClearClaim();
                ShowRegistrationForm();
                ShowError("We couldn't create your account right now. Please try again in a moment.");
                return;
            }

            ClearClaim();

            if (rows != 1)
            {
                ShowRegistrationForm();
                ShowError("An account with this email already exists and is active. Please log in.");
                return;
            }

            Response.Redirect("~/Account/Login?registered=true");
        }

        protected void ClaimCancel_Click(object sender, EventArgs e)
        {
            ClearClaim();
            ShowRegistrationForm();
        }

        private void ShowRegistrationForm()
        {
            pnlRegister.Visible = true;
            pnlClaimVerify.Visible = false;
        }

        private void ClearClaim()
        {
            string[] keys = { "ClaimEmail", "ClaimFirstName", "ClaimSurname", "ClaimPhone", "ClaimClientType",
                              "ClaimPasswordHash", "ClaimOtp", "ClaimOtpExpiry", "ClaimAttempts" };
            foreach (string k in keys) Session.Remove(k);
        }

        /// <summary>Sends the code through the same email service used for login. Type "login" is the template already in use.</summary>
        private static void SendVerificationCode(string email, string otp)
        {
            string webAppUrl = ConfigurationManager.AppSettings["EmailServiceUrl"];
            if (string.IsNullOrEmpty(webAppUrl)) throw new InvalidOperationException("EmailServiceUrl is not configured.");
            string jsonData = new JavaScriptSerializer().Serialize(new { email = email, otp = otp, type = "login" });
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (WebClient client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.UploadString(webAppUrl, "POST", jsonData);
            }
        }

        /// <summary>Cryptographically secure 6-digit code.</summary>
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

        private static bool ConstantTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
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
