using GiantGroupM3.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Owin;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3.Account
{
    public partial class Register : Page
    {
        protected void CreateUser_Click(object sender, EventArgs e)
        {
            if (IsValid)
            {
                string email = Email.Text.Trim();
                string password = Password.Text.Trim();
                string firstName = FirstName.Text.Trim();
                string surname = Surname.Text.Trim();
                string phone = PhoneNumber.Text.Trim();
                string clientType = ClientType.SelectedValue.Trim(); // Captures Residential or Commercial

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    // 1. Check if this email belongs to a Staff member
                    string staffCheckQuery = "SELECT COUNT(*) FROM staff WHERE emailAddress = @Email";
                    using (SqlCommand staffCmd = new SqlCommand(staffCheckQuery, conn))
                    {
                        staffCmd.Parameters.AddWithValue("@Email", email);
                        int staffCount = (int)staffCmd.ExecuteScalar();

                        if (staffCount > 0)
                        {
                            FailureText.Text = "Staff members cannot register through the portal. Please use the standard login page.";
                            ErrorPlaceHolder.Visible = true;
                            return;
                        }
                    }

                    // 2. Check if this email exists in the client table and if they already have a password
                    string clientCheckQuery = "SELECT clientID, passwordHash FROM client WHERE emailAddress = @Email";
                    bool clientExists = false;
                    bool hasPasswordAlready = false;

                    using (SqlCommand clientCmd = new SqlCommand(clientCheckQuery, conn))
                    {
                        clientCmd.Parameters.AddWithValue("@Email", email);
                        using (SqlDataReader reader = clientCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                clientExists = true;
                                object dbHash = reader["passwordHash"];
                                if (dbHash != DBNull.Value && !string.IsNullOrEmpty(dbHash.ToString()))
                                {
                                    hasPasswordAlready = true;
                                }
                            }
                        }
                    }

                    // 3. Handle scenarios based on findings
                    if (hasPasswordAlready)
                    {
                        // Scenario: Client already exists and has already set a password before
                        FailureText.Text = "An account with this email already exists and is active. Please log in.";
                        ErrorPlaceHolder.Visible = true;
                        return;
                    }
                    else if (clientExists)
                    {
                        // Scenario: Client was pre-created by admin in backend, now claiming their account
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
                        // Scenario: Brand new client registration from the web frontend
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

                // Successfully processed, redirect to Login with a success flag
                Response.Redirect("~/Account/Login?registered=true");
            }
        }
    }
}