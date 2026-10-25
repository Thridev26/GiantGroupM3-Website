using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ManageRequests : Page
    {
        private readonly string _connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!IsOperationsAuthorized())
            {
                Response.Redirect("~/Account/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                if (Request.QueryString["message"] == "date")
                {
                    lblMessage.Text = "This request does not have a site evaluation date yet. The Systems Manager must set the date before it can be evaluated.";
                    lblMessage.Visible = true;
                }
                BindRequests("");
            }
        }

        private bool IsOperationsAuthorized()
        {
            if (!Request.IsAuthenticated) return false;

            string role = Convert.ToString(Session["UserRole"] ?? Session["UserType"] ?? "");
            int level = 0;
            int.TryParse(Convert.ToString(Session["UserLevel"] ?? Session["AccessLevel"]), out level);

            // In this system the Operations Manager is access level 4; higher management levels may also access the workflow.
            return level >= 4 || role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Owner", StringComparison.OrdinalIgnoreCase);
        }

        private void BindRequests(string search)
        {
            if (string.IsNullOrWhiteSpace(_connString))
            {
                ShowMessage("The database connection string 'DefaultConnection' is missing from Web.config.");
                return;
            }

            const string sql = @"
                SELECT
                    jr.jobRequestID,
                    c.clientName + ' ' + c.clientSurname AS clientName,
                    jr.siteAddress,
                    jr.siteEvaluationDate,
                    jr.status,
                    STUFF((
                        SELECT ', ' + jt2.jobTypeName
                        FROM dbo.RequestItem ri2
                        INNER JOIN dbo.JobType jt2 ON jt2.jobTypeID = ri2.jobTypeID
                        WHERE ri2.jobRequestID = jr.jobRequestID
                        ORDER BY jt2.jobTypeName
                        FOR XML PATH(''), TYPE
                    ).value('.', 'nvarchar(max)'), 1, 2, '') AS requestedItems
                FROM dbo.JobRequest jr
                INNER JOIN dbo.Client c ON c.clientID = jr.clientID
                WHERE jr.siteEvaluationDate IS NOT NULL
                  AND (
                    @search = ''
                    OR CONVERT(varchar(12), jr.jobRequestID) LIKE @likeSearch
                    OR c.clientName LIKE @likeSearch
                    OR c.clientSurname LIKE @likeSearch
                    OR jr.siteAddress LIKE @likeSearch
                    OR EXISTS (
                        SELECT 1 FROM dbo.RequestItem ri3
                        INNER JOIN dbo.JobType jt3 ON jt3.jobTypeID = ri3.jobTypeID
                        WHERE ri3.jobRequestID = jr.jobRequestID AND jt3.jobTypeName LIKE @likeSearch
                    )
                  )
                ORDER BY CASE WHEN jr.status = 'Site Evaluated' THEN 1 ELSE 0 END,
                         jr.siteEvaluationDate ASC, jr.jobRequestID DESC;";

            try
            {
                DataTable data = new DataTable();
                using (SqlConnection conn = new SqlConnection(_connString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                {
                    string term = (search ?? string.Empty).Trim();
                    cmd.Parameters.Add("@search", SqlDbType.VarChar, 100).Value = term;
                    cmd.Parameters.Add("@likeSearch", SqlDbType.VarChar, 202).Value = "%" + term + "%";
                    adapter.Fill(data);
                }

                gvRequests.DataSource = data;
                gvRequests.DataBind();
                pnlEmpty.Visible = data.Rows.Count == 0;
                gvRequests.Visible = data.Rows.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ManageRequests failed: " + ex);
                ShowMessage("Requests could not be loaded. Please check the database connection and try again.");
            }
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindRequests(txtSearch.Text);
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            lblMessage.Visible = false;
            BindRequests(string.Empty);
        }

        private void ShowMessage(string message)
        {
            lblMessage.Text = HttpUtility.HtmlEncode(message);
            lblMessage.Visible = true;
        }
    }
}
