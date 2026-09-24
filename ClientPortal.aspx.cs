using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ClientPortal : Page
    {
        // Connection string name from Web.config
        private readonly string _connString = ConfigurationManager.ConnectionStrings["defaultConnection"]?.ConnectionString
            ?? @"Server=GroupWst1;Database=GroupWst1;Trusted_Connection=True;";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Security Guard: Ensure user is logged in
            if (Session["ClientID"] == null && Session["Username"] == null)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadClientData();
            }
        }

        private void LoadClientData()
        {
            string clientName = Session["Username"]?.ToString() ?? "Client";
            litClientName.Text = clientName;

            // Get logged-in Client ID from session
            if (int.TryParse(Session["ClientID"]?.ToString(), out int clientID))
            {
                FetchDashboardMetrics(clientID);
            }
            else
            {
                // Fallback / default values if ClientID is missing
                litActiveRequests.Text = "0";
                litQuotesReady.Text = "0";
                litCompletedJobs.Text = "0";
            }
        }

        private void FetchDashboardMetrics(int clientID)
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                conn.Open();

                // 1. Active Requests Count (Status in 'Pending', 'Evaluation Date Set')
                string activeSql = @"
                    SELECT COUNT(*) 
                    FROM [dbo].[JobRequest] 
                    WHERE clientID = @clientID 
                      AND status IN ('Pending', 'Evaluation Date Set');";

                using (SqlCommand cmd = new SqlCommand(activeSql, conn))
                {
                    cmd.Parameters.AddWithValue("@clientID", clientID);
                    litActiveRequests.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }

                // 2. Quotes Ready Count (quoteStatus = 'Sent')
                string quotesReadySql = @"
                    SELECT COUNT(*) 
                    FROM [dbo].[Quote] q
                    INNER JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                    WHERE jr.clientID = @clientID 
                      AND q.quoteStatus = 'Sent';";

                using (SqlCommand cmd = new SqlCommand(quotesReadySql, conn))
                {
                    cmd.Parameters.AddWithValue("@clientID", clientID);
                    litQuotesReady.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }

                // 3. Approved / Accepted Quotes Count (quoteStatus = 'Accepted')
                string approvedQuotesSql = @"
                    SELECT COUNT(*) 
                    FROM [dbo].[Quote] q
                    INNER JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                    WHERE jr.clientID = @clientID 
                      AND q.quoteStatus = 'Accepted';";

                using (SqlCommand cmd = new SqlCommand(approvedQuotesSql, conn))
                {
                    cmd.Parameters.AddWithValue("@clientID", clientID);
                    litCompletedJobs.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                }
            }
        }
    }
}