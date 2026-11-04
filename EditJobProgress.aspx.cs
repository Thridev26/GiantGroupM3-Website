
using System;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class EditJobProgress : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            int level;
            bool validLevel = int.TryParse(
                Convert.ToString(Session["UserLevel"]),
                out level
            );

            bool isStaff = string.Equals(
                Convert.ToString(Session["UserRole"]),
                "Staff",
                StringComparison.OrdinalIgnoreCase
            );

            bool authorised =
                Request.IsAuthenticated &&
                isStaff &&
                validLevel &&
                level >= 4 &&
                level <= 6;

            if (!authorised)
            {
                Response.Redirect("~/Account/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Job data will be loaded here later.
            }
        }
    }
}
