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

        protected void Page_Load(object sender, EventArgs e)
        {
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

        private bool IsUserAuthorized()
        {
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
            object identifier = Session["StaffID"] ?? Session["staffID"] ?? Session["UserName"] ?? Session["userName"];
            if (identifier == null) return false;

            string query = @"
                SELECT r.accessLevel, s.roleID 
                FROM [dbo].[Staff] s
                INNER JOIN [dbo].[Role] r ON s.roleID = r.roleID
                WHERE s.staffID = @ID OR s.userName = @IDStr";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", int.TryParse(identifier.ToString(), out int id) ? id : 0);
                    cmd.Parameters.AddWithValue("@IDStr", identifier.ToString());

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
            }

            return false;
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
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        cmd.Parameters.AddWithValue("@Search", "%" + searchTerm + "%");
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
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        cmd.Parameters.AddWithValue("@Search", "%" + searchTerm + "%");
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
        }

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

        protected void gvJobs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectJob")
            {
                string jobId = e.CommandArgument.ToString();
                hfSelectedJobID.Value = jobId;

                foreach (GridViewRow row in gvJobs.Rows)
                {
                    if (gvJobs.DataKeys[row.RowIndex].Value.ToString() == jobId)
                    {
                        string client = row.Cells[1].Text;
                        string address = row.Cells[2].Text;
                        litSelectedJobDetails.Text = $"Job #{jobId} - {client} ({address})";
                        break;
                    }
                }

                pnlSelectedJob.CssClass = "selected-banner mb-3 d-block";
                pnlJobsGrid.Visible = false;
                ShowMessage("Job selected! Now select an available asset to assign below.", "alert alert-info d-block rounded-3 mb-4");
            }
        }

        protected void btnClearJob_Click(object sender, EventArgs e)
        {
            hfSelectedJobID.Value = string.Empty;
            pnlSelectedJob.CssClass = "selected-banner mb-3 d-none";
            pnlJobsGrid.Visible = true;
            lblMessage.CssClass = "d-none";
        }

        protected void gvAssets_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "AssignAssetToJob")
            {
                if (string.IsNullOrEmpty(hfSelectedJobID.Value))
                {
                    ShowMessage("Please select a job from Step 1 first.", "alert alert-warning d-block rounded-3 mb-4");
                    return;
                }

                string[] args = e.CommandArgument.ToString().Split(':');
                string sourceType = args[0];
                string assetId = args[1];
                string jobId = hfSelectedJobID.Value;

                string insertQuery = @"
                    INSERT INTO [dbo].[JobAssetAssignment] (jobID, ownedAssetID, hiredAssetID, assignmentDate)
                    VALUES (@JobID, @OwnedAssetID, @HiredAssetID, @AssignmentDate)";

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@JobID", jobId);
                        cmd.Parameters.AddWithValue("@OwnedAssetID", sourceType == "Owned" ? (object)assetId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@HiredAssetID", sourceType == "Hired" ? (object)assetId : DBNull.Value);
                        cmd.Parameters.AddWithValue("@AssignmentDate", DateTime.Now.ToString("yyyy-MM-dd"));

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Asset successfully assigned to Job #" + jobId + "!", "alert alert-success d-block rounded-3 mb-4");

                // Refresh asset list so assigned owned assets disappear from 'Available'
                BindAssetsGrid();
            }
        }

        private void ShowMessage(string message, string cssClass)
        {
            lblMessage.Text = message;
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