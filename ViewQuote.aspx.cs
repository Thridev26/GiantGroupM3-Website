using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewQuote : System.Web.UI.Page
    {
        string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Verify authentication via Identity or Session
                if (!User.Identity.IsAuthenticated && Session["UserID"] == null)
                {
                    Response.Redirect("~/Account/Login.aspx");
                    return;
                }

                LoadQuoteData();

                //// If the user is a client, hide the select/edit command column action buttons
                //if (IsClientUser())
                //{
                //    if (gvQuotes.Columns.Count > 0)
                //    {
                //        gvQuotes.Columns[0].Visible = false;
                //    }
                //}
            }
        }

        // Local helper mirroring your access check
        private bool IsClientUser()
        {
            if (Session["UserRole"] != null && Session["UserRole"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Session["Role"] != null && Session["Role"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Session["UserType"] != null && Session["UserType"].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Session["ClientID"] != null)
                return true;

            return false;
        }

        private void LoadQuoteData()
        {
            bool isClient = IsClientUser();
            string query = "";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                if (!isClient)
                {
                    // Management can view all quote entries
                    query = @"SELECT q.QuoteID, q.jobRequestID, c.clientName AS Name, q.dateIssued, q.expiryDate, q.dateGenerated, q.amount, q.quoteStatus, q.filePath 
                              FROM Quote q 
                              JOIN JobRequest jr ON q.jobRequestID = jr.jobRequestID
                              JOIN Client c ON jr.clientID = c.clientID";
                }
                else
                {
                    // Customers only see quotes linked to their account via JobRequest
                    query = @"SELECT q.QuoteID, q.jobRequestID, c.clientName AS Name, q.dateIssued, q.expiryDate, q.dateGenerated, q.amount, q.quoteStatus, q.filePath 
                              FROM Quote q 
                              JOIN JobRequest jr ON q.jobRequestID = jr.jobRequestID
                              JOIN Client c ON jr.clientID = c.clientID
                              WHERE c.clientID = @clientID";
                }

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (isClient)
                    {
                        object clientIdVal = Session["ClientID"] ?? Session["UserID"];
                        cmd.Parameters.AddWithValue("@clientID", clientIdVal ?? (object)DBNull.Value);
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvQuotes.DataSource = dt;
                        gvQuotes.DataBind();
                    }
                }
            }
        }

        protected void gvQuotes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvQuotes.PageIndex = e.NewPageIndex;
            LoadQuoteData();
        }

        protected void gvQuotes_SelectedIndexChanged(object sender, EventArgs e)
        {
            pnlEditQuote.Visible = true;
            int quoteId = Convert.ToInt32(gvQuotes.SelectedDataKey.Value);
            hfQuoteID.Value = quoteId.ToString();

            bool isClient = IsClientUser();

            // Toggle panels depending on whether it's a manager or client view
            pnlManagementControls.Visible = !isClient;
            pnlClientResponse.Visible = isClient;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "SELECT expiryDate, amount, quoteStatus FROM Quote WHERE QuoteID = @QuoteID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@QuoteID", quoteId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtExpiryDate.Text = reader["expiryDate"] != DBNull.Value ? Convert.ToDateTime(reader["expiryDate"]).ToString("yyyy-MM-dd") : "";
                            txtAmount.Text = reader["amount"]?.ToString() ?? "";

                            if (ddlQuoteStatus.Items.FindByValue(reader["quoteStatus"]?.ToString()) != null)
                            {
                                ddlQuoteStatus.SelectedValue = reader["quoteStatus"]?.ToString();
                            }
                        }
                    }
                }
            }
        }

        protected void btnUpdate_Click(object sender, EventArgs e)
        {
            int quoteId = Convert.ToInt32(hfQuoteID.Value);
            DateTime expiryDate = Convert.ToDateTime(txtExpiryDate.Text);
            decimal amount = Convert.ToDecimal(txtAmount.Text);
            string status = ddlQuoteStatus.SelectedValue;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "UPDATE Quote SET expiryDate = @expiryDate, amount = @amount, quoteStatus = @quoteStatus WHERE QuoteID = @QuoteID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@expiryDate", expiryDate);
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@quoteStatus", status);
                    cmd.Parameters.AddWithValue("@QuoteID", quoteId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblMessage.Text = "Quote updated successfully!";
            pnlEditQuote.Visible = false;
            LoadQuoteData();
        }

        protected void btnAcceptQuote_Click(object sender, EventArgs e)
        {
            UpdateQuoteStatus("Accepted", "Quote successfully accepted!");
        }

        protected void btnRejectQuote_Click(object sender, EventArgs e)
        {
            UpdateQuoteStatus("Rejected", "Quote has been rejected.");
        }

        private void UpdateQuoteStatus(string newStatus, string successMessage)
        {
            if (string.IsNullOrEmpty(hfQuoteID.Value)) return;

            int quoteId = Convert.ToInt32(hfQuoteID.Value);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = "UPDATE Quote SET quoteStatus = @quoteStatus WHERE QuoteID = @QuoteID";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@quoteStatus", newStatus);
                    cmd.Parameters.AddWithValue("@QuoteID", quoteId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            lblMessage.Text = successMessage;
            pnlEditQuote.Visible = false;
            LoadQuoteData();
        }
    }
}