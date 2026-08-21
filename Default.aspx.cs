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
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Check if a user is actively authenticated via cookie/login session
                if (User.Identity.IsAuthenticated)
                {
                    // User is logged in! Show the operations hub.
                    OperationsHubPanel.Visible = true;

                    // Hide the guest prompt banner since they are already signed in
                    GuestPromptPanel.Visible = false;

                    // Optional: If you want to differentiate Staff vs Normal Clients further down the line:
                    if (User.IsInRole("Staff") || User.IsInRole("Admin"))
                    {
                        // Staff-specific UI adjustments can go here if needed
                    }
                    else
                    {
                        // Normal client UI adjustments can go here if needed
                    }
                }
                else
                {
                    // Guest user (not logged in): Hide operations, show prompt to log in
                    OperationsHubPanel.Visible = false;
                    GuestPromptPanel.Visible = true;
                }
            }
        }
    }
}