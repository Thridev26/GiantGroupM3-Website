using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
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

            // Never trust coordinates or a reference point supplied by the browser.
            AddressResult verified = LookupAddress(address);
            if (!verified.Success)
            {
                ShowError(verified.Message);
                return;
            }
            if (DistanceMetres(verified.Latitude, verified.Longitude,
                (double)latitude, (double)longitude) > 500.0)
            {
                ShowError("The selected site location must be within 500 metres of the entered address.");
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
                            GiantGroupM3.Services.ClientNotifications.RequestSubmitted(ConnectionString, newRequestId, Request);
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

        public sealed class AddressResult
        {
            public bool Success { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string DisplayName { get; set; }
            public string Message { get; set; }
        }

        private static readonly object GeocodeLock = new object();
        private static DateTime lastLookup = DateTime.MinValue;

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static AddressResult LocateAddress(string address)
        {
            HttpContext context = HttpContext.Current;
            string role = Convert.ToString(context.Session["UserRole"]);
            int clientId;
            if (!context.Request.IsAuthenticated ||
                !int.TryParse(Convert.ToString(context.Session["ClientID"]), out clientId) || clientId <= 0 ||
                (!role.Equals("Client", StringComparison.OrdinalIgnoreCase) &&
                 !role.Equals("Owner", StringComparison.OrdinalIgnoreCase)))
                return new AddressResult { Message = "Please log in again." };
            return LookupAddress(address);
        }

        private static AddressResult LookupAddress(string address)
        {
            address = (address ?? "").Trim();
            if (address.Length < 5 || address.Length > 100)
                return new AddressResult { Message = "Enter a complete site address of no more than 100 characters." };
            string key = "GiantGroup.Geocode." + address.ToUpperInvariant();
            lock (GeocodeLock)
            {
                AddressResult cached = HttpRuntime.Cache[key] as AddressResult;
                if (cached != null) return cached;
                // Limit requests from this application process to one per second.
                if ((DateTime.UtcNow - lastLookup).TotalMilliseconds < 1100)
                    return new AddressResult { Message = "Address lookup is busy. Leave the address field again to retry." };
                lastLookup = DateTime.UtcNow;
                try
                {
                    string url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&countrycodes=za&q=" +
                        Uri.EscapeDataString(address + ", South Africa");
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.UserAgent = "GiantGroup-ClientPortal/1.0";
                    request.Accept = "application/json";
                    request.Headers[HttpRequestHeader.AcceptLanguage] = "en";
                    request.Timeout = 8000;
                    request.ReadWriteTimeout = 8000;
                    using (WebResponse response = request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        JArray results = JArray.Parse(reader.ReadToEnd());
                        double lat, lon;
                        if (results.Count == 0 ||
                            !double.TryParse((string)results[0]["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) ||
                            !double.TryParse((string)results[0]["lon"], NumberStyles.Float, CultureInfo.InvariantCulture, out lon) ||
                            double.IsNaN(lat) || double.IsNaN(lon) || Math.Abs(lat) > 90 || Math.Abs(lon) > 180)
                            return new AddressResult { Message = "Address not found. Add the street number, suburb and town, then try again." };
                        AddressResult found = new AddressResult
                        {
                            Success = true,
                            Latitude = lat,
                            Longitude = lon,
                            DisplayName = (string)results[0]["display_name"] ?? address
                        };
                        HttpRuntime.Cache.Insert(key, found, null, DateTime.UtcNow.AddHours(24), System.Web.Caching.Cache.NoSlidingExpiration);
                        return found;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceWarning("Site address lookup failed: " + ex.Message);
                    return new AddressResult { Message = "Address lookup is temporarily unavailable. Please try again." };
                }
            }
        }

        private static double DistanceMetres(double lat1, double lon1, double lat2, double lon2)
        {
            const double radians = Math.PI / 180.0;
            double dLat = (lat2 - lat1) * radians;
            double dLon = (lon2 - lon1) * radians;
            double a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1 * radians) * Math.Cos(lat2 * radians) * Math.Pow(Math.Sin(dLon / 2), 2);
            return 6371000.0 * 2 * Math.Asin(Math.Sqrt(Math.Min(1.0, Math.Max(0.0, a))));
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
