using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class _Default : Page
    {
        // Public properties so your .aspx markup can easily check them if needed
        public bool IsStaff => Session["StaffName"] != null || (Session["UserLevel"] != null && Convert.ToInt32(Session["UserLevel"]) >= 4);
        public bool IsClient => Session["Username"] != null || (Session["UserLevel"] != null && Convert.ToInt32(Session["UserLevel"]) == 1);
        public bool IsLoggedIn => Request.IsAuthenticated || Session["Username"] != null || Session["StaffName"] != null;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Dynamic visibility control via code-behind panels
                if (IsStaff)
                {
                    if (OperationsHubPanel != null) OperationsHubPanel.Visible = true;
                    if (GuestPromptPanel != null) GuestPromptPanel.Visible = false;
                }
                else
                {
                    if (OperationsHubPanel != null) OperationsHubPanel.Visible = false;
                    if (GuestPromptPanel != null) GuestPromptPanel.Visible = !IsLoggedIn;
                }
            }
        }
    }
}