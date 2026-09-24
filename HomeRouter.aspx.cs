using System;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class HomeRouter : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string userRole = Session["UserRole"]?.ToString() ?? Session["UserType"]?.ToString() ?? string.Empty;
            int userLevel = Convert.ToInt32(Session["UserLevel"] ?? -1);

            // 1. Staff / Admin Routing
            if (userRole.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                userLevel >= 2)
            {
                Response.Redirect("~/ManagementPortal.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // 2. Client Routing (Captures Ethan via Session["ClientID"], UserRole = "Client", or userLevel 0/1)
            if (Session["ClientID"] != null ||
                userRole.Equals("Client", StringComparison.OrdinalIgnoreCase) ||
                userLevel == 0 ||
                userLevel == 1)
            {
                Response.Redirect("~/ClientPortal.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            // 3. Unauthenticated Public Guests
            Response.Redirect("~/Default.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}