using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class AssignAsset : Page
    {
        private string connStr = ConfigurationManager.ConnectionStrings["GroupWst1ConnectionString"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["defaultConnection"]?.ConnectionString;

        // The selected job lives in ViewState (tamper-proof). The hidden field is only kept for compatibility,
        // because a hidden field's value can be edited in the browser.
        private int? SelectedJobId
        {
            get { return ViewState["SelectedJobId"] as int?; }
            set { ViewState["SelectedJobId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!IsUserAuthorized())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindJobsGrid();
                BindAssetsGrid();
            }
        }

        // ------------------------------------------------------------
        // Authorisation (levels 5 / 6 or roles 10 / 11 only)
        // ------------------------------------------------------------
        private bool IsUserAuthorized()
        {
            if (!Request.IsAuthenticated) return false;

            int level = 0;
            int roleId = 0;

            if (Session["AccessLevel"] != null)
                int.TryParse(Session["AccessLevel"].ToString(), out level);
            else if (Session["accessLevel"] != null)
                int.TryParse(Session["accessLevel"].ToString(), out level);

            if (Session["RoleID"] != null)
                int.TryParse(Session["RoleID"].ToString(), out roleId);
            else if (Session["roleID"] != null)
                int.TryParse(Session["roleID"].ToString(), out roleId);

            if (level == 5 || level == 6 || roleId == 10 || roleId == 11)
            {
                return true;
            }

            if (Session["StaffID"] != null || Session["staffID"] != null || Session["UserName"] != null || Session["userName"] != null)
            {
                return CheckUserRoleFromDatabase();
            }

            return false;
        }

        private bool CheckUserRoleFromDatabase()
        {
            object staffIdObj = Session["StaffID"] ?? Session["staffID"];
            object userNameObj = Session["UserName"] ?? Session["userName"];

            // Prefer the numeric StaffID. A username that happens to look like a number must never match a different staff ID.
            bool byId = staffIdObj != null && int.TryParse(staffIdObj.ToString(), out int staffId);
            if (!byId && userNameObj == null) return false;

            string query = @"
                SELECT r.accessLevel, s.roleID 
                FROM [dbo].[Staff] s
                INNER JOIN [dbo].[Role] r ON s.roleID = r.roleID
                WHERE " + (byId ? "s.staffID = @ID" : "s.userName = @IDStr");

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (byId) cmd.Parameters.Add("@ID", SqlDbType.Int).Value = int.Parse(staffIdObj.ToString());
                else cmd.Parameters.Add("@IDStr", SqlDbType.NVarChar, 100).Value = userNameObj.ToString();

                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        int accessLevel = dr["accessLevel"] != DBNull.Value ? Convert.ToInt32(dr["accessLevel"]) : 0;
                        int roleId = dr["roleID"] != DBNull.Value ? Convert.ToInt32(dr["roleID"]) : 0;
                        return accessLevel == 5 || accessLevel == 6 || roleId == 10 || roleId == 11;
                    }
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        // Data binding
        // ------------------------------------------------------------
        private static string LikeTerm(string s)
        {
            // Escape SQL LIKE wildcards so a search for "%" or "_" finds those characters instead of everything
            return "%" + s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        }

        private void BindJobsGrid()
        {
            string searchTerm = txtSearchJobs.Text.Trim();

            string query = @"
                SELECT 
                    j.jobID, 
                    j.startDate,
                    ISNULL(c.clientName + ' ' + c.clientSurname, 'N/A') AS ClientName,
                    ISNULL(jr.siteAddress, 'N/A') AS siteAddress
                FROM [dbo].[Job] j
                LEFT JOIN [dbo].[Quote] q ON j.quoteID = q.quoteID
                LEFT JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                LEFT JOIN [dbo].[Client] c ON jr.clientID = c.clientID
                WHERE j.jobStatus != 'Archived' ";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += @" AND (
                    CAST(j.jobID AS VARCHAR) LIKE @Search OR 
                    c.clientName LIKE @Search OR 
                    c.clientSurname LIKE @Search OR 
                    jr.siteAddress LIKE @Search
                ) ";
            }

            query += " ORDER BY j.jobID ASC";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", LikeTerm(searchTerm));
                }

                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvJobs.DataSource = dt;
                    gvJobs.DataBind();
                }
            }
        }

        private void BindAssetsGrid()
        {
            string searchTerm = txtSearchAssets.Text.Trim();

            // UNION query bringing together Available Owned Assets and Hired Assets into one clean list
            string query = @"
                SELECT 
                    'Owned' AS SourceType,
                    assetID AS AssetID,
                    type AS EquipmentType,
                    ISNULL(serialNumber, 'N/A') AS Identifier,
                    'Giant Group (Owned)' AS SupplierOrOwner
                FROM [dbo].[OwnedAsset]
                WHERE assetStatus = 'Available' ";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (type LIKE @Search OR serialNumber LIKE @Search) ";
            }

            query += @"
                UNION ALL

                SELECT 
                    'Hired' AS SourceType,
                    hiredAssetID AS AssetID,
                    equipmentType AS EquipmentType,
                    'Hired Asset #' + CAST(hiredAssetID AS VARCHAR) AS Identifier,
                    ISNULL(supplierName, 'External Supplier') AS SupplierOrOwner
                FROM [dbo].[HiredAsset]
                WHERE 1=1 ";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (equipmentType LIKE @Search OR supplierName LIKE @Search) ";
            }

            query += " ORDER BY SourceType ASC, EquipmentType ASC";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", LikeTerm(searchTerm));
                }

                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvAssets.DataSource = dt;
                    gvAssets.DataBind();
                }
            }
        }

        // Assign buttons stay disabled until a job has been chosen
        protected void gvAssets_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var btn = e.Row.FindControl("btnAssignAsset") as LinkButton;
                if (btn != null) btn.Enabled = SelectedJobId.HasValue;
            }
        }

        // ------------------------------------------------------------
        // Search handlers
        // ------------------------------------------------------------
        protected void txtSearchJobs_TextChanged(object sender, EventArgs e)
        {
            gvJobs.PageIndex = 0;
            BindJobsGrid();
        }

        protected void btnSearchJobs_Click(object sender, EventArgs e)
        {
            gvJobs.PageIndex = 0;
            BindJobsGrid();
        }

        protected void txtSearchAssets_TextChanged(object sender, EventArgs e)
        {
            gvAssets.PageIndex = 0;
            BindAssetsGrid();
        }

        protected void btnSearchAssets_Click(object sender, EventArgs e)
        {
            gvAssets.PageIndex = 0;
            BindAssetsGrid();
        }

        // ------------------------------------------------------------
        // Step 1: choose a job
        // ------------------------------------------------------------
        protected void gvJobs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "SelectJob") return;

            int jobId;
            if (!int.TryParse(e.CommandArgument.ToString(), out jobId) || !JobIsOpen(jobId))
            {
                ShowMessage("That job is no longer available. Please pick another one.", "gg-msg is-warning");
                BindJobsGrid();
                return;
            }

            SelectedJobId = jobId;
            hfSelectedJobID.Value = jobId.ToString();

            foreach (GridViewRow row in gvJobs.Rows)
            {
                if (gvJobs.DataKeys[row.RowIndex].Value.ToString() == jobId.ToString())
                {
                    // BoundField cell text is already HTML-encoded by ASP.NET
                    string client = row.Cells[1].Text;
                    string address = row.Cells[2].Text;
                    litSelectedJobDetails.Text = $"Job #{jobId} - {client} ({address})";
                    break;
                }
            }

            pnlSelectedJob.CssClass = "gg-selected";
            pnlJobsGrid.Visible = false;
            BindAssetsGrid(); // re-bind so the Assign buttons unlock
            ShowMessage("Job selected! Now select an available asset to assign below.", "gg-msg is-info");
        }

        protected void btnClearJob_Click(object sender, EventArgs e)
        {
            SelectedJobId = null;
            hfSelectedJobID.Value = string.Empty;
            pnlSelectedJob.CssClass = "gg-selected d-none";
            pnlJobsGrid.Visible = true;
            lblMessage.CssClass = "d-none";
            BindJobsGrid();
            BindAssetsGrid(); // re-lock the Assign buttons
        }

        private bool JobIsOpen(int jobId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[Job] WHERE jobID = @JobID AND jobStatus != 'Archived'", conn))
            {
                cmd.Parameters.Add("@JobID", SqlDbType.Int).Value = jobId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // ------------------------------------------------------------
        // Step 2: assign an asset
        // ------------------------------------------------------------
        protected void gvAssets_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "AssignAssetToJob") return;

            if (!SelectedJobId.HasValue)
            {
                ShowMessage("Please select a job from Step 1 first.", "gg-msg is-warning");
                return;
            }

            string[] args = e.CommandArgument.ToString().Split(':');
            int assetId;
            if (args.Length != 2 || (args[0] != "Owned" && args[0] != "Hired") || !int.TryParse(args[1], out assetId))
            {
                ShowMessage("That asset could not be read. Please refresh the page and try again.", "gg-msg is-error");
                return;
            }

            bool owned = args[0] == "Owned";
            int jobId = SelectedJobId.Value;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // The asset must still exist (and an owned asset must still be Available)
                    string availSql = owned
                        ? "SELECT COUNT(1) FROM [dbo].[OwnedAsset] WHERE assetID = @A AND assetStatus = 'Available'"
                        : "SELECT COUNT(1) FROM [dbo].[HiredAsset] WHERE hiredAssetID = @A";
                    using (SqlCommand check = new SqlCommand(availSql, conn))
                    {
                        check.Parameters.Add("@A", SqlDbType.Int).Value = assetId;
                        if (Convert.ToInt32(check.ExecuteScalar()) == 0)
                        {
                            ShowMessage("That asset is no longer available.", "gg-msg is-warning");
                            BindAssetsGrid();
                            return;
                        }
                    }

                    // Don't assign the same asset to the same job twice on the same day
                    string dupSql = "SELECT COUNT(1) FROM [dbo].[JobAssetAssignment] WHERE jobID = @J AND assignmentDate = @D AND "
                                    + (owned ? "ownedAssetID = @A" : "hiredAssetID = @A");
                    using (SqlCommand dup = new SqlCommand(dupSql, conn))
                    {
                        dup.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                        dup.Parameters.Add("@A", SqlDbType.Int).Value = assetId;
                        dup.Parameters.Add("@D", SqlDbType.Date).Value = DateTime.Today;
                        if (Convert.ToInt32(dup.ExecuteScalar()) > 0)
                        {
                            ShowMessage("This asset is already assigned to Job #" + jobId + " today.", "gg-msg is-warning");
                            return;
                        }
                    }

                    string insertQuery = @"
                        INSERT INTO [dbo].[JobAssetAssignment] (jobID, ownedAssetID, hiredAssetID, assignmentDate)
                        VALUES (@JobID, @OwnedAssetID, @HiredAssetID, @AssignmentDate)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.Add("@JobID", SqlDbType.Int).Value = jobId;
                        cmd.Parameters.Add("@OwnedAssetID", SqlDbType.Int).Value = owned ? (object)assetId : DBNull.Value;
                        cmd.Parameters.Add("@HiredAssetID", SqlDbType.Int).Value = owned ? DBNull.Value : (object)assetId;
                        cmd.Parameters.Add("@AssignmentDate", SqlDbType.Date).Value = DateTime.Today;
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Asset successfully assigned to Job #" + jobId + "!", "gg-msg is-success");
                BindAssetsGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("AssignAsset failed: " + ex);
                ShowMessage("We couldn't save that assignment. Please try again.", "gg-msg is-error");
            }
        }

        private void ShowMessage(string message, string cssClass)
        {
            lblMessage.Text = Server.HtmlEncode(message);
            lblMessage.CssClass = cssClass;
        }

        protected void gvJobs_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvJobs.PageIndex = e.NewPageIndex;
            BindJobsGrid();
        }

        protected void gvAssets_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvAssets.PageIndex = e.NewPageIndex;
            BindAssetsGrid();
        }
    }
}