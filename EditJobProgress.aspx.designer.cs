
namespace GiantGroupM3
{
    public partial class EditJobProgress
    {
        protected global::System.Web.UI.WebControls.Label lblMessage;

        protected global::System.Web.UI.WebControls.DropDownList ddlJobs;
        protected global::System.Web.UI.WebControls.Button btnLoadJob;

        protected global::System.Web.UI.WebControls.TextBox txtJobID;
        protected global::System.Web.UI.WebControls.TextBox txtQuoteID;
        protected global::System.Web.UI.WebControls.TextBox txtClientName;
        protected global::System.Web.UI.WebControls.TextBox txtScheduledDate;
        protected global::System.Web.UI.WebControls.TextBox txtSiteAddress;
        protected global::System.Web.UI.WebControls.TextBox txtTimeSlots;
        protected global::System.Web.UI.WebControls.TextBox txtCurrentStatus;

        protected global::System.Web.UI.WebControls.DropDownList ddlNewStatus;

        protected global::System.Web.UI.WebControls.Panel pnlNotStarted;
        protected global::System.Web.UI.WebControls.Panel pnlInProgress;
        protected global::System.Web.UI.WebControls.Panel pnlCompleted;

        protected global::System.Web.UI.WebControls.Panel pnlCompletion;

        protected global::System.Web.UI.WebControls.TextBox txtCompletionDate;
        protected global::System.Web.UI.WebControls.FileUpload fuCompletionPhoto;
        protected global::System.Web.UI.WebControls.Label lblPhotoInfo;

        protected global::System.Web.UI.WebControls.Button btnSaveProgress;
        protected global::System.Web.UI.WebControls.Button btnResetProgress;
    }
}

