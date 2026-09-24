using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class AssignStaff : Page
    {
        private string connStr = ConfigurationManager.ConnectionStrings["GroupWst1ConnectionString"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

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
                BindStaffGrid();
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

        private void BindStaffGrid()
        {
            string searchTerm = txtSearchStaff.Text.Trim();

            // Exclude System Managers (Level 5 / Role 10) and Owners (Level 6 / Role 11)
            string query = @"
                SELECT 
                    s.staffID, 
                    s.firstName,
                    s.lastName,
                    s.contactNumber,
                    s.emailAddress,
                    ISNULL(r.roleName, 'General') AS roleName
                FROM [dbo].[Staff] s
                LEFT JOIN [dbo].[Role] r ON s.roleID = r.roleID
                WHERE s.staffStatus = 'Active'
                  AND (r.accessLevel NOT IN (5, 6) OR r.accessLevel IS NULL)
                  AND (s.roleID NOT IN (10, 11) OR s.roleID IS NULL) ";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += @" AND (
                    s.firstName LIKE @Search OR 
                    s.lastName LIKE @Search OR 
                    s.emailAddress LIKE @Search OR 
                    s.contactNumber LIKE @Search OR 
                    r.roleName LIKE @Search
                ) ";
            }

            query += " ORDER BY s.firstName ASC";

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
                        gvStaff.DataSource = dt;
                        gvStaff.DataBind();
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

        protected void txtSearchStaff_TextChanged(object sender, EventArgs e)
        {
            gvStaff.PageIndex = 0;
            BindStaffGrid();
        }

        protected void btnSearchStaff_Click(object sender, EventArgs e)
        {
            gvStaff.PageIndex = 0;
            BindStaffGrid();
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

                pnlSelectedJob.CssClass = "selected-job-banner mb-3 d-block";
                pnlJobsGrid.Visible = false;
                ShowMessage("Job selected! Now select an operational staff member to assign below.", "alert alert-info d-block rounded-3 mb-4");
            }
        }

        protected void btnClearJob_Click(object sender, EventArgs e)
        {
            hfSelectedJobID.Value = string.Empty;
            pnlSelectedJob.CssClass = "selected-job-banner mb-3 d-none";
            pnlJobsGrid.Visible = true;
            lblMessage.CssClass = "d-none";
        }

        protected void gvStaff_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "AssignStaffMember")
            {
                if (string.IsNullOrEmpty(hfSelectedJobID.Value))
                {
                    ShowMessage("Please select a job from Step 1 first.", "alert alert-warning d-block rounded-3 mb-4");
                    return;
                }

                string staffId = e.CommandArgument.ToString();
                string jobId = hfSelectedJobID.Value;

                string insertQuery = @"
                    INSERT INTO [dbo].[JobStaffAssignment] (staffID, jobID, hoursWorked, assignmentDate)
                    VALUES (@StaffID, @JobID, @HoursWorked, @AssignmentDate)";

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StaffID", staffId);
                        cmd.Parameters.AddWithValue("@JobID", jobId);
                        cmd.Parameters.AddWithValue("@HoursWorked", 8.00m);
                        cmd.Parameters.AddWithValue("@AssignmentDate", DateTime.Now.ToString("yyyy-MM-dd"));

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                ShowMessage("Staff member successfully assigned to Job #" + jobId + "!", "alert alert-success d-block rounded-3 mb-4");
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

        protected void gvStaff_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvStaff.PageIndex = e.NewPageIndex;
            BindStaffGrid();
        }
    }
}