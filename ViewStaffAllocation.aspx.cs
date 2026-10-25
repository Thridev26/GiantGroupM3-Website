using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewStaffAllocation : Page
    {
        private readonly string connStr = ConfigurationManager.ConnectionStrings["GroupWst1ConnectionString"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // Must be signed in (clients see only their own jobs, staff see their own, management sees all)
            if (!Request.IsAuthenticated)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadStaffAllocations();
            }
        }

        private static int ToInt(object value)
        {
            int result;
            return value != null && int.TryParse(value.ToString(), out result) ? result : 0;
        }

        private void LoadStaffAllocations(string searchTerm = "")
        {
            int accessLevel = ToInt(Session["AccessLevel"]);
            int roleID = ToInt(Session["RoleID"]);
            string userRole = Session["UserRole"]?.ToString() ?? "";
            int clientID = ToInt(Session["ClientID"]);
            int staffID = ToInt(Session["StaffID"]);

            // FALLBACK: If AccessLevel/RoleID were not set in Session, look them up directly from database
            if (staffID > 0 && (accessLevel == 0 || roleID == 0))
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string lookupQuery = @"
                        SELECT s.roleID, r.accessLevel, r.roleName 
                        FROM [dbo].[Staff] s 
                        INNER JOIN [dbo].[Role] r ON s.roleID = r.roleID 
                        WHERE s.staffID = @StaffID";

                    using (SqlCommand cmd = new SqlCommand(lookupQuery, conn))
                    {
                        cmd.Parameters.Add("@StaffID", SqlDbType.Int).Value = staffID;
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                roleID = Convert.ToInt32(reader["roleID"]);
                                accessLevel = Convert.ToInt32(reader["accessLevel"]);
                                userRole = reader["roleName"].ToString();

                                // Update Session so future page loads remember it
                                Session["RoleID"] = roleID;
                                Session["AccessLevel"] = accessLevel;
                                Session["UserRole"] = userRole;
                            }
                        }
                    }
                }
            }

            // Owner, System Manager, or AccessLevel >= 5 sees everything
            bool isManagement = accessLevel >= 5
                || roleID == 10
                || roleID == 11
                || userRole.Equals("Owner", StringComparison.OrdinalIgnoreCase)
                || userRole.Equals("System Manager", StringComparison.OrdinalIgnoreCase)
                || userRole.Equals("Systems Manager", StringComparison.OrdinalIgnoreCase);

            string query = @"
                SELECT 
                    jsa.staffID,
                    jsa.jobID,
                    jsa.assignmentDate,
                    ISNULL(j.startDate, jsa.assignmentDate) AS startDate,
                    ISNULL(j.jobStatus, 'Pending') AS jobStatus,
                    ISNULL(s.firstName + ' ' + s.lastName, 'Staff #' + CAST(jsa.staffID AS VARCHAR)) AS StaffName,
                    ISNULL(c.clientName + ' ' + c.clientSurname, 'N/A') AS ClientName,
                    ISNULL(jr.siteAddress, 'N/A') AS SiteAddress
                FROM [dbo].[JobStaffAssignment] jsa
                LEFT JOIN [dbo].[Staff] s ON jsa.staffID = s.staffID
                LEFT JOIN [dbo].[Job] j ON jsa.jobID = j.jobID
                LEFT JOIN [dbo].[Quote] q ON j.quoteID = q.quoteID
                LEFT JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                LEFT JOIN [dbo].[Client] c ON jr.clientID = c.clientID
                WHERE 1=1";

            // Only management sees everything. Everyone else is restricted to their own records.
            if (!isManagement)
            {
                if (userRole.Equals("Client", StringComparison.OrdinalIgnoreCase) || clientID > 0)
                {
                    query += " AND jr.clientID = @ClientID";
                }
                else if (staffID > 0)
                {
                    query += " AND jsa.staffID = @StaffID";
                }
                else
                {
                    // Unknown identity: show nothing rather than everything
                    query += " AND 1 = 0";
                }
            }

            // Escape LIKE wildcards in what the user typed
            string term = string.IsNullOrWhiteSpace(searchTerm) ? "" : searchTerm.Trim()
                .Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

            if (term.Length > 0)
            {
                query += @" AND (s.firstName LIKE '%' + @Search + '%' 
                            OR s.lastName LIKE '%' + @Search + '%' 
                            OR c.clientName LIKE '%' + @Search + '%'
                            OR c.clientSurname LIKE '%' + @Search + '%'
                            OR jr.siteAddress LIKE '%' + @Search + '%'
                            OR j.jobStatus LIKE '%' + @Search + '%'
                            OR CAST(jsa.jobID AS VARCHAR) LIKE '%' + @Search + '%')";
            }

            query += " ORDER BY jsa.assignmentDate DESC";

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = term;
                    cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = clientID;
                    cmd.Parameters.Add("@StaffID", SqlDbType.Int).Value = staffID;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvStaffAssignments.DataSource = dt;
                        gvStaffAssignments.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ViewStaffAllocation load failed: " + ex);
            }
        }

        protected string GetStatusBadgeCss(object statusObj)
        {
            if (statusObj == null || statusObj == DBNull.Value)
                return "status-default";

            switch (statusObj.ToString().Trim().ToLower())
            {
                case "completed":
                    return "status-completed";
                case "in progress":
                case "inprogress":
                case "active":
                case "scheduled":
                    return "status-inprogress";
                case "pending":
                    return "status-pending";
                case "archived":
                    return "status-archived";
                default:
                    return "status-default";
            }
        }

        protected void gvStaffAssignments_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvStaffAssignments.PageIndex = e.NewPageIndex;
            LoadStaffAllocations(txtSearch.Text);   // keep the current search while paging
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            gvStaffAssignments.PageIndex = 0;
            LoadStaffAllocations(txtSearch.Text);
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            gvStaffAssignments.PageIndex = 0;
            LoadStaffAllocations();
        }
    }
}