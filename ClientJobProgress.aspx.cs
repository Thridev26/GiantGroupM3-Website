using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{
    public partial class ClientJobProgress : Page
    {
        private readonly string _connString =
            ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

        private int CurrentClientID
        {
            get
            {
                int id;
                return int.TryParse(Session["ClientID"]?.ToString(), out id) ? id : 0;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!Request.IsAuthenticated || CurrentClientID <= 0)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (string.IsNullOrWhiteSpace(_connString))
                throw new ConfigurationErrorsException("The DefaultConnection connection string is missing from Web.config.");

            if (!IsPostBack)
                LoadJobs();
        }

        private void LoadJobs()
        {
            // Read scheduled jobs from Job, then follow the same relationships as the backend:
            // Job -> Quote -> JobRequest -> Client, plus JobTimeSlot -> TimeSlot.
            // The client filter is applied to the linked JobRequest for the signed-in client.
            const string sql = @"
                SELECT
                    j.jobID AS JobID,
                    jr.siteAddress AS SiteAddress,
                    COALESCE(NULLIF(j.jobStatus, ''), 'Not specified') AS JobStatus,
                    COALESCE(CONVERT(varchar(11), j.startDate, 106), 'Not scheduled') AS JobDateDisplay,
                    COALESCE(
                        CONVERT(varchar(5), ts.startTime, 108) + ' - ' +
                        CONVERT(varchar(5), ts.endTime, 108),
                        'Time not assigned'
                    ) AS TimeSlotDisplay
                FROM dbo.Job AS j
                INNER JOIN dbo.Quote AS q ON q.QuoteID = j.quoteID
                INNER JOIN dbo.JobRequest AS jr ON jr.jobRequestID = q.jobRequestID
                LEFT JOIN dbo.JobTimeSlot AS jts ON jts.jobID = j.jobID
                LEFT JOIN dbo.TimeSlot AS ts ON ts.timeSlotID = jts.timeSlotID
                WHERE jr.clientID = @clientID
                ORDER BY j.startDate DESC, j.jobID DESC, ts.startTime ASC;";

            var table = new DataTable();
            try
            {
                using (var conn = new SqlConnection(_connString))
                using (var cmd = new SqlCommand(sql, conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = CurrentClientID;
                    adapter.Fill(table);
                }

                rptJobs.DataSource = table;
                rptJobs.DataBind();
                pnlEmpty.Visible = table.Rows.Count == 0;
                pnlError.Visible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ClientJobProgress load failed: " + ex);
                pnlError.Visible = true;
                pnlEmpty.Visible = false;
            }
        }
    }
}
