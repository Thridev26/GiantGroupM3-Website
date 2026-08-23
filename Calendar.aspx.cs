using System;

namespace GiantGroupM3
{
    public partial class Calendar : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Security check matching your nav bar condition
            if (!CanSeeCalendar())
            {
                Response.Redirect("~/Default.aspx");
            }
        }

        // Helper method matching your role check logic
        private bool CanSeeCalendar()
        {
            // Implement your role-checking session or authentication logic here
            // e.g., return Session["UserRole"] != null && Session["UserRole"].ToString() == "Staff";
            return true; // Set to true for development/testing visibility
        }
    }
}