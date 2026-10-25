using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3.Requests
{
    public partial class ViewRequests : Page
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
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!Request.IsAuthenticated || ClientId <= 0 ||
                !string.Equals(Convert.ToString(Session["UserRole"]), "Client", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect(ResolveUrl("~/Account/Login.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
                BindRequests();
        }

        private void BindRequests()
        {
            const string sql = @"
                SELECT
                    jr.jobRequestID,
                    jr.dateRecieved,
                    jr.siteAddress,
                    jr.urgencyLevel,
                    COALESCE(jr.status, 'Pending') AS RequestStatus,
                    jr.siteEvaluationDate,
                    STUFF((
                        SELECT ', ' + jt.jobTypeName
                        FROM dbo.RequestItem AS ri
                        INNER JOIN dbo.JobType AS jt ON jt.jobTypeID = ri.jobTypeID
                        WHERE ri.jobRequestID = jr.jobRequestID
                        ORDER BY jt.jobTypeName
                        FOR XML PATH(''), TYPE
                    ).value('.', 'nvarchar(max)'), 1, 2, '') AS JobTypes
                FROM dbo.JobRequest AS jr
                WHERE jr.clientID = @ClientID
                ORDER BY jr.dateRecieved DESC, jr.jobRequestID DESC;";

            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                using (SqlCommand command = new SqlCommand(sql, connection))
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    command.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                    DataTable requests = new DataTable();
                    adapter.Fill(requests);
                    gvRequests.DataSource = requests;
                    gvRequests.DataBind();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Client request history failed: " + ex);
                pnlError.Visible = true;
            }
        }
    }
}
