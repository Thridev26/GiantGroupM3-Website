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
            if (!IsPostBack)
            {
                LoadAssetAllocations();
            }
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

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(searchTerm) ? "" : searchTerm.Trim());
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gvAssetAssignments.DataSource = dt;
                            gvAssetAssignments.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log ex.Message here to inspect backend database errors
                System.Diagnostics.Debug.WriteLine("Database Error: " + ex.Message);
            }
        }
        protected void gvAssetAssignments_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvAssetAssignments.PageIndex = e.NewPageIndex;
            LoadAssetAllocations();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            LoadAssetAllocations(txtSearch.Text);
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            LoadAssetAllocations();
        }
    }
}