using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewJobProgress : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(
                System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!Request.IsAuthenticated)
            {
                Response.Redirect(
                    ResolveUrl("~/Account/Login.aspx"),
                    false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            string role = Convert.ToString(Session["UserRole"]);

            int level;
            bool validLevel = int.TryParse(
                Convert.ToString(Session["UserLevel"]),
                out level);

            bool isStaff = string.Equals(
                role,
                "Staff",
                StringComparison.OrdinalIgnoreCase);

            bool authorised =
                isStaff &&
                validLevel &&
                level >= 4 &&
                level <= 6;

            if (!authorised)
            {
                Response.StatusCode = 403;
                Response.TrySkipIisCustomErrors = true;
                Response.Write(
                    "Access denied. This page requires " +
                    "Operations Manager access or higher.");
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Database loading will be added later.
            }
        }

    }
}
