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
            if (!IsPostBack)
            {
                LoadStaffAllocations();
            }
        }

        private void LoadStaffAllocations(string searchTerm = "")
        {
            int accessLevel = Session["AccessLevel"] != null ? Convert.ToInt32(Session["AccessLevel"]) : 0;
            int roleID = Session["RoleID"] != null ? Convert.ToInt32(Session["RoleID"]) : 0;
            string userRole = Session["UserRole"]?.ToString() ?? "";
            int clientID = Session["ClientID"] != null ? Convert.ToInt32(Session["ClientID"]) : 0;
            int staffID = Session["StaffID"] != null ? Convert.ToInt32(Session["StaffID"]) : 0;

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
                        cmd.Parameters.AddWithValue("@StaffID", staffID);
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

            // Check if user is Owner, System Manager, or has AccessLevel >= 5
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

            // Only filter by staffID/clientID if NOT management
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
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
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

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", searchTerm.Trim());
                    cmd.Parameters.AddWithValue("@ClientID", clientID);
                    cmd.Parameters.AddWithValue("@StaffID", staffID);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvStaffAssignments.DataSource = dt;
                        gvStaffAssignments.DataBind();
                    }
                }
            }
        }

        protected string GetStatusBadgeCss(object statusObj)
        {
            if (statusObj == null || statusObj == DBNull.Value)
                return "badge-status status-default";

            string status = statusObj.ToString().Trim().ToLower();

            switch (status)
            {
                case "completed":
                    return "badge-status status-completed";
                case "in progress":
                case "inprogress":
                    return "badge-status status-inprogress";
                case "pending":
                    return "badge-status status-pending";
                case "archived":
                    return "badge-status status-archived";
                default:
                    return "badge-status status-default";
            }
        }

        protected void gvStaffAssignments_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvStaffAssignments.PageIndex = e.NewPageIndex;
            LoadStaffAllocations(txtSearch.Text);
        }
    }
}