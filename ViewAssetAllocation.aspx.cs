using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewAssetAllocation : Page
    {
        private readonly string connStr = ConfigurationManager.ConnectionStrings["GroupWst1ConnectionString"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // This page lists every client's name and site address, so it is for signed-in staff only
            // (the menu already hides it from clients; this enforces it on the server too).
            if (!Request.IsAuthenticated)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }
            if (IsClientUser())
            {
                Response.Redirect("~/Default.aspx");
                return;
            }

            if (!IsPostBack)
            {
                LoadAssetAllocations();
            }
        }

        private bool IsClientUser()
        {
            string role = Session["UserRole"]?.ToString() ?? Session["Role"]?.ToString() ?? Session["UserType"]?.ToString() ?? "";
            return role.Equals("Client", StringComparison.OrdinalIgnoreCase) || Session["ClientID"] != null;
        }

        private void LoadAssetAllocations(string searchTerm = "")
        {
            string query = @"
        SELECT 
            a.AssignmentID,
            a.jobID,
            a.ownedAssetID,
            a.hiredAssetID,
            a.assignmentDate,
            h.returnDate AS ReturnDate,
            COALESCE(
                o.type + ' (' + o.serialNumber + ')', 
                h.equipmentType, 
                'Asset #' + CAST(ISNULL(a.ownedAssetID, a.hiredAssetID) AS VARCHAR)
            ) AS AssetName,
            CASE 
                WHEN a.ownedAssetID IS NOT NULL THEN 'Owned Equipment'
                WHEN a.hiredAssetID IS NOT NULL THEN 'Hired Equipment'
                ELSE 'Unspecified'
            END AS AssetSource,
            'Job #' + CAST(a.jobID AS VARCHAR) AS JobName,
            ISNULL(c.clientName + ' ' + c.clientSurname, 'N/A') AS ClientName,
            ISNULL(jr.siteAddress, 'N/A') AS SiteAddress
        FROM [dbo].[JobAssetAssignment] a
        LEFT JOIN [dbo].[OwnedAsset] o ON a.ownedAssetID = o.assetID
        LEFT JOIN [dbo].[HiredAsset] h ON a.hiredAssetID = h.hiredAssetID
        LEFT JOIN [dbo].[Job] j ON a.jobID = j.jobID
        LEFT JOIN [dbo].[Quote] q ON j.quoteID = q.quoteID
        LEFT JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
        LEFT JOIN [dbo].[Client] c ON jr.clientID = c.clientID
        WHERE (@Search = '' 
               OR CAST(a.jobID AS VARCHAR) LIKE '%' + @Search + '%'
               OR CAST(a.AssignmentID AS VARCHAR) LIKE '%' + @Search + '%'
               OR o.type LIKE '%' + @Search + '%' 
               OR o.serialNumber LIKE '%' + @Search + '%' 
               OR h.equipmentType LIKE '%' + @Search + '%'
               OR c.clientName LIKE '%' + @Search + '%'
               OR c.clientSurname LIKE '%' + @Search + '%'
               OR jr.siteAddress LIKE '%' + @Search + '%')
        ORDER BY a.assignmentDate DESC";

            // Escape LIKE wildcards in what the user typed
            string term = string.IsNullOrWhiteSpace(searchTerm) ? "" : searchTerm.Trim()
                .Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = term;
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvAssetAssignments.DataSource = dt;
                        gvAssetAssignments.DataBind();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ViewAssetAllocation load failed: " + ex);
            }
        }

        protected void gvAssetAssignments_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvAssetAssignments.PageIndex = e.NewPageIndex;
            LoadAssetAllocations(txtSearch.Text);   // keep the current search while paging
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            gvAssetAssignments.PageIndex = 0;
            LoadAssetAllocations(txtSearch.Text);
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            gvAssetAssignments.PageIndex = 0;
            LoadAssetAllocations(txtSearch.Text);
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            gvAssetAssignments.PageIndex = 0;
            LoadAssetAllocations();
        }
    }
}