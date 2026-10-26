using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
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

        private void ShowMessage(string message, bool isError)
        {
            litMessage.Text = HttpUtility.HtmlEncode(message);
            pnlMessage.Visible = true;
            pnlMessage.CssClass = isError ? "gg-alert mb-3" : "gg-alert mb-3";
        }
    }
}
