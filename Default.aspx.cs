using System;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class _Default : Page
    {
        // Public helper properties for ASPX inline evaluation
        public bool IsLoggedIn => Request.IsAuthenticated || Session["UserRole"] != null;

        public bool IsStaff
        {
            get;
            private set;
        }

        public bool IsClient
        {
            get;
            private set;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            DetermineUserRole();

            if (!IsPostBack)
            {
                ConfigureDashboardLayout();
            }
        }

        private void DetermineUserRole()
        {
            string role = Session["UserRole"]?.ToString() ?? string.Empty;
            int level = 0;

            if (Session["UserLevel"] != null)
            {
                int.TryParse(Session["UserLevel"].ToString(), out level);
            }

            // Exclusively categorize role context to prevent UI overlap
            if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Owner", StringComparison.OrdinalIgnoreCase) ||
                level >= 4)
            {
                IsStaff = true;
                IsClient = false;
            }
            else if (role.Equals("Client", StringComparison.OrdinalIgnoreCase) || level == 1)
            {
                IsClient = true;
                IsStaff = false;
            }
            else
            {
                IsStaff = false;
                IsClient = false;
            }
        }

        private void ConfigureDashboardLayout()
        {
            if (IsStaff)
            {
                if (OperationsHubPanel != null) OperationsHubPanel.Visible = true;
                if (ClientHubPanel != null) ClientHubPanel.Visible = false;
                if (GuestPromptPanel != null) GuestPromptPanel.Visible = false;
            }
            else if (IsClient)
            {
                if (OperationsHubPanel != null) OperationsHubPanel.Visible = false;
                if (ClientHubPanel != null) ClientHubPanel.Visible = true;
                if (GuestPromptPanel != null) GuestPromptPanel.Visible = false;
            }
            else
            {
                if (OperationsHubPanel != null) OperationsHubPanel.Visible = false;
                if (ClientHubPanel != null) ClientHubPanel.Visible = false;
                if (GuestPromptPanel != null) GuestPromptPanel.Visible = true;
            }
        }
    }
}