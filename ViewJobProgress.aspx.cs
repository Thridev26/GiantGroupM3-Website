
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewJobProgress : Page
    {
        private string ConnectionString
        {
            get
            {
                ConnectionStringSettings setting =
                    ConfigurationManager.ConnectionStrings[
                        "DefaultConnection"];

                if (setting == null ||
                    string.IsNullOrWhiteSpace(
                        setting.ConnectionString))
                {
                    throw new ConfigurationErrorsException(
                        "DefaultConnection is not configured.");
                }

                return setting.ConnectionString;
            }
        }

        // ============================================
        // PAGE LOAD AND PERMISSIONS
        // ============================================

        protected void Page_Load(
            object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(
                HttpCacheability.NoCache);

            Response.Cache.SetNoStore();

            if (!Request.IsAuthenticated)
            {
                Response.Redirect(
                    ResolveUrl("~/Account/Login.aspx"),
                    true);

                return;
            }

            SiteMaster siteMaster = Master as SiteMaster;

            if (siteMaster == null)
            {
                throw new HttpException(
                    500, "Master page is unavailable.");
            }

            int level = siteMaster.GetUserLevel();

            bool isStaff =
                !siteMaster.IsClient() &&
                (
                    Session["StaffID"] != null ||
                    string.Equals(
                        Convert.ToString(Session["UserRole"]),
                        "Staff",
                        StringComparison.OrdinalIgnoreCase)
                );

            if (!isStaff || level < 4 || level > 6)
            {
                throw new HttpException(
                    403,
                    "Operations Manager access or higher " +
                    "is required to view all jobs.");
            }

            if (!IsPostBack)
            {
                pnlNoSelection.Visible = true;
                pnlJobDetails.Visible = false;

                try
                {
                    LoadSummary();
                    LoadJobs();

                    // Optional direct link:
                    // ViewJobProgress.aspx?jobId=12
                    int requestedId;

                    if (int.TryParse(
                        Request.QueryString["jobId"],
                        out requestedId) &&
                        requestedId > 0)
                    {
                        LoadJobDetails(requestedId);
                    }
                }
                catch (Exception ex)
                {
                    LogError("Page_Load", ex);
                    ShowError(
                        "Unable to load job progress. " +
                        "Please check the database connection.");
                }
            }
        }

        // ============================================
        // MESSAGE HELPERS
        // ============================================

        private void ShowError(string message)
        {
            pnlError.Visible = true;
            lblError.Text = Server.HtmlEncode(message);
        }

        private void ClearError()
        {
            pnlError.Visible = false;
            lblError.Text = "";
        }

        private void LogError(
            string operation, Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                "ViewJobProgress / " +
                operation + ": " + ex);
        }

        // ============================================
        // STATUS CSS
        // ============================================

        protected string StatusCss(object statusObject)
        {
            string status =
                Convert.ToString(statusObject);

            switch (status)
            {
                case "Not Started":
                    return "vjp-status vjp-pending";

                case "In Progress":
                    return "vjp-status vjp-active";

                case "Completed":
                    return "vjp-status vjp-completed";

                case "Archived":
                    return "vjp-status vjp-archived";

                default:
                    return "vjp-status";
            }
        }

        // ============================================
        // SUMMARY TOTALS
        // ============================================

        private void LoadSummary()
        {
            const string sql = @"
                SELECT
                    COUNT(*) AS TotalJobs,

                    ISNULL(SUM(
                        CASE WHEN jobStatus = 'Not Started'
                        THEN 1 ELSE 0 END), 0)
                        AS NotStarted,

                    ISNULL(SUM(
                        CASE WHEN jobStatus = 'In Progress'
                        THEN 1 ELSE 0 END), 0)
                        AS InProgress,

                    ISNULL(SUM(
                        CASE WHEN jobStatus = 'Completed'
                        THEN 1 ELSE 0 END), 0)
                        AS Completed

                FROM dbo.Job;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        lblTotalJobs.Text =
                            Convert.ToString(
                                reader["TotalJobs"]);

                        lblNotStarted.Text =
                            Convert.ToString(
                                reader["NotStarted"]);

                        lblInProgress.Text =
                            Convert.ToString(
                                reader["InProgress"]);

                        lblCompleted.Text =
                            Convert.ToString(
                                reader["Completed"]);
                    }
                }
            }
        }

        // ============================================
        // DATE FILTER PARSING
        // ============================================

        private DateTime? ParseFilterDate(
            string input, string description)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            DateTime date;

            if (!DateTime.TryParseExact(
                input,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
            {
                throw new ArgumentException(
                    "Invalid " + description + ".");
            }

            return date.Date;
        }

        // ============================================
        // LOAD JOB RECORDS
        // ============================================

        private void LoadJobs()
        {
            string status =
                ddlStatusFilter.SelectedValue;

            if (status != "All" &&
                status != "Not Started" &&
                status != "In Progress" &&
                status != "Completed" &&
                status != "Archived")
            {
                status = "All";
            }

            string search =
                (txtSearchJobs.Text ?? "").Trim();

            if (search.Length > 100)
            {
                search = search.Substring(0, 100);
            }

            DateTime? fromDate = ParseFilterDate(
                txtFromDate.Text, "From Date");

            DateTime? toDate = ParseFilterDate(
                txtToDate.Text, "To Date");

            if (fromDate.HasValue &&
                toDate.HasValue &&
                fromDate.Value > toDate.Value)
            {
                throw new ArgumentException(
                    "From Date cannot be later than To Date.");
            }

            // The OUTER APPLY combines several time slots
            // into one displayed value per job.
            //
            // This prevents duplicate job rows.

            const string sql = @"
                SELECT
                    j.jobID,
                    j.jobStatus,
                    jr.siteAddress,

                    CONCAT(
                        c.clientName,
                        ' ',
                        c.clientSurname
                    ) AS ClientName,

                    COALESCE(
                        CONVERT(VARCHAR(11),
                            j.startDate, 106),
                        'Not scheduled'
                    ) AS ScheduledDateDisplay,

                    COALESCE(
                        slotData.TimeSlots,
                        'Not assigned'
                    ) AS TimeSlots

                FROM dbo.Job j

                INNER JOIN dbo.Quote q
                    ON j.quoteID = q.QuoteID

                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID

                INNER JOIN dbo.Client c
                    ON jr.clientID = c.clientID

                OUTER APPLY
                (
                    SELECT
                        STUFF
                        (
                            (
                                SELECT
                                    ', ' +
                                    CONVERT(VARCHAR(5),
                                        ts.startTime, 108)
                                    + ' - ' +
                                    CONVERT(VARCHAR(5),
                                        ts.endTime, 108)

                                FROM dbo.JobTimeSlot jts

                                INNER JOIN dbo.TimeSlot ts
                                    ON jts.timeSlotID =
                                       ts.timeSlotID

                                WHERE jts.jobID = j.jobID

                                ORDER BY ts.startTime

                                FOR XML PATH(''), TYPE
                            ).value('.', 'NVARCHAR(MAX)'),
                            1, 2, ''
                        ) AS TimeSlots
                ) slotData

                WHERE
                    (@status = 'All'
                        OR j.jobStatus = @status)

                    AND
                    (
                        @search = ''
                        OR CAST(j.jobID AS VARCHAR(20))
                            LIKE @pattern
                        OR c.clientName LIKE @pattern
                        OR c.clientSurname LIKE @pattern
                        OR jr.siteAddress LIKE @pattern
                    )

                    AND
                    (
                        @fromDate IS NULL
                        OR j.startDate >= @fromDate
                    )

                    AND
                    (
                        @toDate IS NULL
                        OR j.startDate <= @toDate
                    )

                ORDER BY
                    j.startDate DESC,
                    j.jobID DESC;";

            DataTable table = new DataTable();

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@status",
                    SqlDbType.VarChar, 30)
                    .Value = status;

                command.Parameters.Add(
                    "@search",
                    SqlDbType.VarChar, 100)
                    .Value = search;

                command.Parameters.Add(
                    "@pattern",
                    SqlDbType.VarChar, 210)
                    .Value = "%" + search + "%";

                command.Parameters.Add(
                    "@fromDate",
                    SqlDbType.Date)
                    .Value = fromDate.HasValue
                        ? (object)fromDate.Value
                        : DBNull.Value;

                command.Parameters.Add(
                    "@toDate",
                    SqlDbType.Date)
                    .Value = toDate.HasValue
                        ? (object)toDate.Value
                        : DBNull.Value;

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }

            gvJobs.DataSource = table;
            gvJobs.DataBind();

            lblRecordCount.Text =
                table.Rows.Count + " record(s)";

            ClearError();
        }

        // ============================================
        // APPLY FILTERS
        // ============================================

        protected void btnApplyFilters_Click(
            object sender, EventArgs e)
        {
            try
            {
                gvJobs.PageIndex = 0;
                gvJobs.SelectedIndex = -1;

                HideJobDetails();
                LoadJobs();
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                LogError("Apply filters", ex);

                ShowError(
                    "Unable to filter the job records.");
            }
        }

        // ============================================
        // CLEAR FILTERS
        // ============================================

        protected void btnClearFilters_Click(
            object sender, EventArgs e)
        {
            txtSearchJobs.Text = "";
            ddlStatusFilter.SelectedValue = "All";
            txtFromDate.Text = "";
            txtToDate.Text = "";

            gvJobs.PageIndex = 0;
            gvJobs.SelectedIndex = -1;

            HideJobDetails();

            try
            {
                LoadJobs();
                ClearError();
            }
            catch (Exception ex)
            {
                LogError("Clear filters", ex);

                ShowError(
                    "Unable to reload all jobs.");
            }
        }

        // ============================================
        // GRID PAGINATION
        // ============================================

        protected void gvJobs_PageIndexChanging(
            object sender,
            GridViewPageEventArgs e)
        {
            gvJobs.PageIndex = e.NewPageIndex;
            gvJobs.SelectedIndex = -1;

            HideJobDetails();

            try
            {
                LoadJobs();
            }
            catch (Exception ex)
            {
                LogError("Grid pagination", ex);

                ShowError(
                    "Unable to load the next page of jobs.");
            }
        }

        // ============================================
        // SELECT JOB FROM GRID
        // ============================================

        protected void gvJobs_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            if (gvJobs.SelectedDataKey == null)
            {
                ShowError(
                    "Select a valid job.");
                return;
            }

            int jobId;

            if (!int.TryParse(
                Convert.ToString(
                    gvJobs.SelectedDataKey.Value),
                out jobId))
            {
                ShowError("Invalid job selection.");
                return;
            }

            try
            {
                LoadJobDetails(jobId);
                ClearError();
            }
            catch (Exception ex)
            {
                LogError("Select job", ex);

                HideJobDetails();

                ShowError(
                    "Unable to load the selected job.");
            }
        }

        // ============================================
        // LOAD JOB DETAILS
        // ============================================

        private void LoadJobDetails(int jobId)
        {
            const string sql = @"
                SELECT
                    j.jobID,
                    j.quoteID,
                    j.jobStatus,
                    j.startDate,
                    j.totalFuelCost,
                    j.dumpingCost,

                    c.clientName,
                    c.clientSurname,
                    jr.siteAddress

                FROM dbo.Job j

                INNER JOIN dbo.Quote q
                    ON j.quoteID = q.QuoteID

                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID

                INNER JOIN dbo.Client c
                    ON jr.clientID = c.clientID

                WHERE j.jobID = @jobID;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                string status;

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@jobID", SqlDbType.Int)
                        .Value = jobId;

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException(
                                "Job not found.");
                        }

                        lblJobID.Text =
                            Convert.ToString(
                                reader["jobID"]);

                        lblQuoteID.Text =
                            Convert.ToString(
                                reader["quoteID"]);

                        string client =
                            Convert.ToString(
                                reader["clientName"]) +
                            " " +
                            Convert.ToString(
                                reader["clientSurname"]);

                        lblClientName.Text =
                            Server.HtmlEncode(client);

                        lblSiteAddress.Text =
                            Server.HtmlEncode(
                                Convert.ToString(
                                    reader["siteAddress"]));

                        lblStartDate.Text =
                            reader["startDate"] == DBNull.Value
                            ? "Not scheduled"
                            : Convert.ToDateTime(
                                reader["startDate"])
                                .ToString("dd MMM yyyy");

                        decimal fuel =
                            Convert.ToDecimal(
                                reader["totalFuelCost"]);

                        decimal dumping =
                            Convert.ToDecimal(
                                reader["dumpingCost"]);

                        lblFuelCost.Text =
                            "R " + fuel.ToString(
                                "N2",
                                CultureInfo.GetCultureInfo(
                                    "en-ZA"));

                        lblDumpingCost.Text =
                            "R " + dumping.ToString(
                                "N2",
                                CultureInfo.GetCultureInfo(
                                    "en-ZA"));

                        status =
                            Convert.ToString(
                                reader["jobStatus"]);

                        lblCurrentStatus.Text =
                            Server.HtmlEncode(status);
                    }
                }

                // Load all assigned time slots.
                LoadJobTimeSlots(connection, jobId);

                // Load saved completion evidence.
                LoadCompletionEvidence(connection, jobId);

                // Display current progress.
                UpdateProgressTracker(status);

                // Editing is handled on the separate
                // Edit Job Progress page.
                lnkEditProgress.Visible =
                    status == "Not Started" ||
                    status == "In Progress";

                if (lnkEditProgress.Visible)
                {
                    lnkEditProgress.NavigateUrl =
                        ResolveUrl(
                            "~/EditJobProgress.aspx?jobId=" +
                            jobId.ToString());
                }
            }

            pnlJobDetails.Visible = true;
            pnlNoSelection.Visible = false;
        }

        // ============================================
        // LOAD ASSIGNED JOB TIME SLOTS
        // ============================================

        private void LoadJobTimeSlots(
            SqlConnection connection,
            int jobId)
        {
            const string sql = @"
                SELECT
                    ts.startTime,
                    ts.endTime

                FROM dbo.JobTimeSlot jts

                INNER JOIN dbo.TimeSlot ts
                    ON jts.timeSlotID = ts.timeSlotID

                WHERE jts.jobID = @jobID

                ORDER BY ts.startTime;";

            List<string> slots =
                new List<string>();

            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@jobID", SqlDbType.Int)
                    .Value = jobId;

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TimeSpan start =
                            (TimeSpan)reader["startTime"];

                        TimeSpan end =
                            (TimeSpan)reader["endTime"];

                        slots.Add(
                            start.ToString(@"hh\:mm") +
                            " - " +
                            end.ToString(@"hh\:mm"));
                    }
                }
            }

            lblTimeSlots.Text =
                slots.Count == 0
                ? "Not assigned"
                : Server.HtmlEncode(
                    string.Join(", ", slots));
        }

        // ============================================
        // LOAD COMPLETION EVIDENCE
        // ============================================

        private void LoadCompletionEvidence(
            SqlConnection connection,
            int jobId)
        {
            const string sql = @"
                SELECT
                    COUNT(*) AS PhotoCount,
                    MAX(uploadDate) AS LatestPhotoDate

                FROM dbo.SitePhoto

                WHERE jobID = @jobID
                  AND photoType = 'AFTER';";

            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@jobID", SqlDbType.Int)
                    .Value = jobId;

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int count =
                            Convert.ToInt32(
                                reader["PhotoCount"]);

                        lblPhotoCount.Text =
                            count.ToString();

                        lblPhotoDate.Text =
                            reader["LatestPhotoDate"] ==
                                DBNull.Value
                            ? "Not recorded"
                            : Convert.ToDateTime(
                                reader["LatestPhotoDate"])
                                .ToString("dd MMM yyyy");
                    }
                }
            }
        }

        // ============================================
        // UPDATE VISUAL PROGRESS TRACKER
        // ============================================

        private void UpdateProgressTracker(
            string status)
        {
            pnlStageNotStarted.CssClass =
                "vjp-stage";

            pnlStageInProgress.CssClass =
                "vjp-stage";

            pnlStageCompleted.CssClass =
                "vjp-stage";

            switch (status)
            {
                case "Not Started":
                    pnlStageNotStarted.CssClass =
                        "vjp-stage is-current";

                    lblProgressNote.Text =
                        "This job has been scheduled " +
                        "but work has not started.";
                    break;

                case "In Progress":
                    pnlStageNotStarted.CssClass =
                        "vjp-stage is-done";

                    pnlStageInProgress.CssClass =
                        "vjp-stage is-current";

                    lblProgressNote.Text =
                        "This job is currently in progress.";
                    break;

                case "Completed":
                    pnlStageNotStarted.CssClass =
                        "vjp-stage is-done";

                    pnlStageInProgress.CssClass =
                        "vjp-stage is-done";

                    pnlStageCompleted.CssClass =
                        "vjp-stage is-current";

                    lblProgressNote.Text =
                        "This job has been marked Completed.";
                    break;

                case "Archived":
                    lblProgressNote.Text =
                        "This job is archived. The database " +
                        "does not record its previous " +
                        "progress stage.";
                    break;

                default:
                    lblProgressNote.Text =
                        "Progress status is unavailable.";
                    break;
            }
        }

        // ============================================
        // HIDE DETAILS
        // ============================================

        private void HideJobDetails()
        {
            pnlJobDetails.Visible = false;
            pnlNoSelection.Visible = true;

            lnkEditProgress.Visible = false;
        }
    }
}
