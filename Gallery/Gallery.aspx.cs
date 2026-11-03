using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;

namespace GiantGroupM3
{
    // Intentionally public: no login check. Only published projects and public-facing fields are queried.
    public partial class Gallery : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;
            string cs = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (var cn = new SqlConnection(cs))
            using (var cmd = new SqlCommand(@"
                SELECT GalleryID, Title, Category, Description
                FROM dbo.PublicJobGallery WHERE IsPublished = 1
                ORDER BY DisplayOrder ASC, CreatedAtUtc DESC, GalleryID DESC", cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                var dt = new DataTable();
                da.Fill(dt);
                EmptyPanel.Visible = dt.Rows.Count == 0;
                GalleryRepeater.DataSource = dt;
                GalleryRepeater.DataBind();
            }
        }
    }
}
