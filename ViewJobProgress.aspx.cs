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

            int level;
            string role = Convert.ToString(Session["UserRole"]);

            bool allowed =
                Request.IsAuthenticated &&
                int.TryParse(Convert.ToString(Session["UserLevel"]), out level) &&
                level >= 4 && level <= 6 &&
                string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);

            if (!allowed)
            {
                Response.Redirect("~/Account/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

        }
    }
}