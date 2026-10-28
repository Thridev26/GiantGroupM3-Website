using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3.Requests
{
    public partial class SubmitRequest : Page
    {
        private string ConnectionString
        {
            get
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
                if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                    throw new ConfigurationErrorsException("The DefaultConnection connection string is missing from Web.config.");
                return settings.ConnectionString;
            }
        }

        private int ClientId
        {
            get
            {
                int clientId;
                if (!int.TryParse(Convert.ToString(Session["ClientID"], CultureInfo.InvariantCulture), out clientId))
                    return 0;
                return clientId;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            string role = Convert.ToString(Session["UserRole"]);
            if (!Request.IsAuthenticated || ClientId <= 0 ||
     (!role.Equals("Client", StringComparison.OrdinalIgnoreCase) &&
      !role.Equals("Owner", StringComparison.OrdinalIgnoreCase)))
            {
                Response.Redirect(ResolveUrl("~/Account/Login.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                BindJobTypes();
                ShowSuccessIfOwnedRequest();
            }
        }

        private void BindJobTypes()
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT jobTypeID, jobTypeName FROM dbo.JobType ORDER BY jobTypeName;", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    cblJobTypes.DataSource = reader;
                    cblJobTypes.DataValueField = "jobTypeID";
                    cblJobTypes.DataTextField = "jobTypeName";
                    cblJobTypes.DataBind();
                }
            }
        }

        protected void ValidateJobTypes(object source, ServerValidateEventArgs args)
        {
            args.IsValid = cblJobTypes.Items.Count > 0 && cblJobTypes.Items.Cast<ListItem>().Any(item => item.Selected);
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            // Recheck authorization in the event handler as well as Page_Load.
            string role = Convert.ToString(Session["UserRole"]);

            if (!Request.IsAuthenticated || ClientId <= 0 ||
                (!role.Equals("Client", StringComparison.OrdinalIgnoreCase) &&
                 !role.Equals("Owner", StringComparison.OrdinalIgnoreCase)))
            {
                Response.Redirect(ResolveUrl("~/Account/Login.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            pnlError.Visible = false;
            pnlSuccess.Visible = false;

            Page.Validate();
            if (!Page.IsValid)
                return;

            string address = (txtSiteAddress.Text ?? string.Empty).Trim();
            if (address.Length == 0 || address.Length > 100)
            {
                ShowError("Enter a site address of no more than 100 characters.");
                return;
            }

            decimal latitude;
            decimal longitude;
            if (!decimal.TryParse(hfLatitude.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) ||
                !decimal.TryParse(hfLongitude.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) ||
                latitude < -90m || latitude > 90m || longitude < -180m || longitude > 180m)
            {
                ShowError("Select the site location on the map before submitting your request.");
                return;
            }

            List<int> selectedJobTypeIds = new List<int>();
            foreach (ListItem item in cblJobTypes.Items)
            {
                if (item.Selected)
                {
                    int jobTypeId;
                    if (!int.TryParse(item.Value, NumberStyles.None, CultureInfo.InvariantCulture, out jobTypeId) || jobTypeId <= 0)
                    {
                        ShowError("One of the selected job types is invalid. Refresh the page and try again.");
                        return;
                    }
                    selectedJobTypeIds.Add(jobTypeId);
                }
            }

            if (selectedJobTypeIds.Count == 0)
            {
                ShowError("Select at least one job type.");
                return;
            }

            int newRequestId;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            const string insertRequestSql = @"
                                INSERT INTO dbo.JobRequest
                                    (clientID, siteAddress, longitude, latitude, requestSource, urgencyLevel)
                                VALUES
                                    (@ClientID, @SiteAddress, @Longitude, @Latitude, @RequestSource, @UrgencyLevel);
                                SELECT CAST(SCOPE_IDENTITY() AS int);";

                            using (SqlCommand command = new SqlCommand(insertRequestSql, connection, transaction))
                            {
                                command.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                                command.Parameters.Add("@SiteAddress", SqlDbType.VarChar, 100).Value = address;

                                SqlParameter longitudeParameter = command.Parameters.Add("@Longitude", SqlDbType.Decimal);
                                longitudeParameter.Precision = 9;
                                longitudeParameter.Scale = 6;
                                longitudeParameter.Value = longitude;

                                SqlParameter latitudeParameter = command.Parameters.Add("@Latitude", SqlDbType.Decimal);
                                latitudeParameter.Precision = 9;
                                latitudeParameter.Scale = 6;
                                latitudeParameter.Value = latitude;

                                command.Parameters.Add("@RequestSource", SqlDbType.VarChar, 20).Value = "Website";
                                command.Parameters.Add("@UrgencyLevel", SqlDbType.VarChar, 15).Value = ddlUrgency.SelectedValue;
                                newRequestId = Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
                            }

                            // Validate every posted job type against the database and add one RequestItem row per type.
                            const string insertItemSql = @"
                                INSERT INTO dbo.RequestItem (jobRequestID, jobTypeID)
                                SELECT @JobRequestID, jobTypeID
                                FROM dbo.JobType
                                WHERE jobTypeID = @JobTypeID;";

                            foreach (int jobTypeId in selectedJobTypeIds)
                            {
                                using (SqlCommand command = new SqlCommand(insertItemSql, connection, transaction))
                                {
                                    command.Parameters.Add("@JobRequestID", SqlDbType.Int).Value = newRequestId;
                                    command.Parameters.Add("@JobTypeID", SqlDbType.Int).Value = jobTypeId;
                                    if (command.ExecuteNonQuery() != 1)
                                        throw new InvalidOperationException("A selected job type no longer exists.");
                                }
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                // Redirect after POST so refreshing the confirmation page will not submit a duplicate request.
                Response.Redirect(ResolveUrl("~/Requests/SubmitRequest.aspx?submitted=" + newRequestId.ToString(CultureInfo.InvariantCulture)), false);
                Context.ApplicationInstance.CompleteRequest();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Client job request submission failed: " + ex);
                ShowError("We could not submit your request just now. Please try again. If the problem continues, contact support.");
            }
        }

        private void ShowSuccessIfOwnedRequest()
        {
            int requestId;
            if (!int.TryParse(Request.QueryString["submitted"], NumberStyles.None, CultureInfo.InvariantCulture, out requestId) || requestId <= 0)
                return;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.JobRequest WHERE jobRequestID = @RequestID AND clientID = @ClientID;", connection))
            {
                command.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                command.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                connection.Open();
                if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1)
                {
                    litRequestId.Text = requestId.ToString(CultureInfo.InvariantCulture);
                    pnlSuccess.Visible = true;
                }
            }
        }

        private void ShowError(string message)
        {
            litError.Text = message;
            pnlError.Visible = true;
        }
    }
}
