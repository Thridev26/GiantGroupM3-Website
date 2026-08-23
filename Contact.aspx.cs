using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class Contact : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            // Here you can add your logic to process the form or send an email to info@gianttreefelling.co.za
            if (Page.IsValid)
            {
                pnlSuccess.Visible = true;

                // Clear form fields
                txtName.Text = string.Empty;
                txtEmail.Text = string.Empty;
                txtPhone.Text = string.Empty;
                txtMessage.Text = string.Empty;
            }
        }
    }
}
    