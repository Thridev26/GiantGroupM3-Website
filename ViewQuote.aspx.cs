using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewQuote : System.Web.UI.Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

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
            }
        }

        // Helper mirroring access check
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
                    query = @"SELECT q.QuoteID, q.jobRequestID, c.clientName AS Name, q.dateIssued, q.expiryDate, q.dateGenerated, q.amount, q.quoteStatus, q.filePath 
                              FROM Quote q 
                              JOIN JobRequest jr ON q.jobRequestID = jr.jobRequestID
                              JOIN Client c ON jr.clientID = c.clientID
                              WHERE q.quoteStatus IN ('Sent', 'Accepted', 'Rejected')";
                }
                else
                {
                    query = @"SELECT q.QuoteID, q.jobRequestID, c.clientName AS Name, q.dateIssued, q.expiryDate, q.dateGenerated, q.amount, q.quoteStatus, q.filePath 
                              FROM Quote q 
                              JOIN JobRequest jr ON q.jobRequestID = jr.jobRequestID
                              JOIN Client c ON jr.clientID = c.clientID
                              WHERE q.quoteStatus IN ('Sent', 'Accepted', 'Rejected') AND c.clientID = @clientID";
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

                        // Handle empty state visibility
                        if (dt.Rows.Count == 0)
                        {
                            gvQuotes.Visible = false;
                            pnlEmptyState.Visible = true;
                        }
                        else
                        {
                            gvQuotes.Visible = true;
                            pnlEmptyState.Visible = false;
                        }
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

        private void UpdateQuoteStatus(string newQuoteStatus, string successMessage)
        {
            if (string.IsNullOrEmpty(hfQuoteID.Value)) return;

            int quoteId = Convert.ToInt32(hfQuoteID.Value);

            // Map Quote status to the exact allowed JobRequest CHECK constraint value
            string jobStatusToSet;
            if (newQuoteStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            {
                jobStatusToSet = "Quote Accepted";
            }
            else if (newQuoteStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                jobStatusToSet = "Cancelled";
            }
            else
            {
                jobStatusToSet = "Quote sent";
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Update the Quote status
                        string updateQuoteQuery = "UPDATE Quote SET quoteStatus = @quoteStatus WHERE QuoteID = @QuoteID";
                        using (SqlCommand cmdQuote = new SqlCommand(updateQuoteQuery, conn, transaction))
                        {
                            cmdQuote.Parameters.AddWithValue("@quoteStatus", newQuoteStatus);
                            cmdQuote.Parameters.AddWithValue("@QuoteID", quoteId);
                            cmdQuote.ExecuteNonQuery();
                        }

                        // 2. Sync corresponding JobRequest status using allowed constraint value
                        string updateJobQuery = @"UPDATE JobRequest 
                                                  SET status = @jobStatus 
                                                  WHERE jobRequestID = (SELECT jobRequestID FROM Quote WHERE QuoteID = @QuoteID)";
                        using (SqlCommand cmdJob = new SqlCommand(updateJobQuery, conn, transaction))
                        {
                            cmdJob.Parameters.AddWithValue("@jobStatus", jobStatusToSet);
                            cmdJob.Parameters.AddWithValue("@QuoteID", quoteId);
                            cmdJob.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            lblMessage.Text = successMessage;
            pnlEditQuote.Visible = false;
            LoadQuoteData();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            // Hide edit/response card
            pnlEditQuote.Visible = false;

            // Reset selected index on GridView
            gvQuotes.SelectedIndex = -1;

            // Clear feedback messages if any
            lblMessage.Text = string.Empty;
        }
    }
}