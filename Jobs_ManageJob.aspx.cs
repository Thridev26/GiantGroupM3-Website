
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class Jobs_ManageJob : Page
    {
        private const int MaxPhotoBytes = 15 * 1024 * 1024;

        private string ConnectionString
        {
            get
            {
                var setting = ConfigurationManager
                    .ConnectionStrings["DefaultConnection"];

                if (setting == null ||
                    string.IsNullOrWhiteSpace(setting.ConnectionString))
                {
                    throw new InvalidOperationException(
                        "DefaultConnection is not configured.");
                }

                return setting.ConnectionString;
            }
        }

        private int SelectedJobId
        {
            get
            {
                object value = ViewState["JM_JobId"];
                return value == null ? 0 : (int)value;
            }
            set { ViewState["JM_JobId"] = value; }
        }

        private int SelectedQuoteId
        {
            get
            {
                object value = ViewState["JM_QuoteId"];
                return value == null ? 0 : (int)value;
            }
            set { ViewState["JM_QuoteId"] = value; }
        }

        // -------------------------------------------------
        // PAGE LOAD AND SECURITY
        // -------------------------------------------------

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(
                HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!Request.IsAuthenticated)
            {
                Response.Redirect(
                    ResolveUrl("~/Account/Login.aspx"),
                    false);

                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            int level;
            bool hasLevel = int.TryParse(
                Convert.ToString(Session["UserLevel"]),
                out level);

            bool isStaff = string.Equals(
                Convert.ToString(Session["UserRole"]),
                "Staff",
                StringComparison.OrdinalIgnoreCase);

            if (!isStaff || !hasLevel ||
                level < 4 || level > 6)
            {
                Response.StatusCode = 403;
                Response.TrySkipIisCustomErrors = true;
                Response.Write(
                    "Access denied. Operations Manager " +
                    "access or higher is required.");
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                try
                {
                    BindAcceptedQuotes();
                    BindExistingJobs();
                    BindJobsGrid();

                    ClearFormFields();
                    LoadAvailableSlots(
                        DateTime.Today, 0);
                }
                catch (Exception ex)
                {
                    LogError("Page_Load", ex);
                    ShowError(
                        "Unable to load Jobs data. " +
                        "Check the database connection.");
                }
            }
        }

        // -------------------------------------------------
        // SQL HELPERS
        // -------------------------------------------------

        private static object Scalar(
            SqlConnection connection,
            SqlTransaction transaction,
            string sql,
            params SqlParameter[] parameters)
        {
            using (SqlCommand command =
                new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null &&
                    parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                return command.ExecuteScalar();
            }
        }

        private static int Execute(
            SqlConnection connection,
            SqlTransaction transaction,
            string sql,
            params SqlParameter[] parameters)
        {
            using (SqlCommand command =
                new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null &&
                    parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                return command.ExecuteNonQuery();
            }
        }

        private static SqlParameter IntParam(
            string name, int value)
        {
            return new SqlParameter(name, SqlDbType.Int)
            {
                Value = value
            };
        }

        private static SqlParameter DateParam(
            string name, DateTime value)
        {
            return new SqlParameter(name, SqlDbType.Date)
            {
                Value = value.Date
            };
        }

        private static SqlParameter MoneyParam(
            string name, decimal value)
        {
            return new SqlParameter(name,
                SqlDbType.Decimal)
            {
                Precision = 10,
                Scale = 2,
                Value = value
            };
        }

        private void LogError(string area, Exception error)
        {
            System.Diagnostics.Trace.TraceError(
                "Jobs_ManageJob / " + area + ": " +
                error.ToString());
        }

        private void ShowSuccess(string message)
        {
            lblMessage.Visible = true;
            lblMessage.CssClass = "gg-msg is-success";
            lblMessage.Text = Server.HtmlEncode(message);
        }

        private void ShowError(string message)
        {
            lblMessage.Visible = true;
            lblMessage.CssClass = "gg-msg is-error";
            lblMessage.Text = Server.HtmlEncode(message);
        }

        private void ShowWarning(string message)
        {
            lblMessage.Visible = true;
            lblMessage.CssClass = "gg-msg is-warning";
            lblMessage.Text = Server.HtmlEncode(message);
        }

        // -------------------------------------------------
        // LOAD ACCEPTED QUOTATIONS
        // -------------------------------------------------





        private void BindAcceptedQuotes()
        {
            ddlAcceptedQuote.Items.Clear();

            ddlAcceptedQuote.Items.Add(
                new ListItem(
                    "-- Select accepted quotation --", ""));

            const string sql = @"
        SELECT
            q.QuoteID,
            q.jobRequestID,
            c.clientName,
            c.clientSurname,
            jr.siteAddress

        FROM dbo.Quote q

        INNER JOIN dbo.JobRequest jr
            ON q.jobRequestID = jr.jobRequestID

        INNER JOIN dbo.Client c
            ON jr.clientID = c.clientID

        WHERE q.quoteStatus = 'Accepted'

          AND ISNULL(jr.status, '') <> 'Cancelled'

          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Job j
              WHERE j.quoteID = q.QuoteID
          )

        ORDER BY q.QuoteID DESC;";

            int count = 0;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int quoteId =
                            Convert.ToInt32(reader["QuoteID"]);

                        int requestId =
                            Convert.ToInt32(reader["jobRequestID"]);

                        string clientName =
                            Convert.ToString(reader["clientName"]) +
                            " " +
                            Convert.ToString(reader["clientSurname"]);

                        string address =
                            Convert.ToString(reader["siteAddress"]);

                        string display =
                            "GT-" + requestId +
                            " | " + clientName +
                            " | " + address +
                            " | Accepted";

                        ddlAcceptedQuote.Items.Add(
                            new ListItem(
                                display,
                                quoteId.ToString()));

                        count++;
                    }
                }
            }

            if (count == 0)
            {
                lblQuoteInfo.Text =
                    "No accepted quotations are available. " +
                    "Quotations must be accepted and not " +
                    "already linked to a job.";
            }
            else
            {
                lblQuoteInfo.Text =
                    count + " accepted quotation(s) " +
                    "available for job creation.";
            }
        }




        // -------------------------------------------------
        // LOAD EXISTING JOBS DROPDOWN
        // -------------------------------------------------

        private void BindExistingJobs()
        {
            ddlExistingJob.Items.Clear();

            ddlExistingJob.Items.Add(
                new ListItem(
                    "-- Select existing job --", ""));

            const string sql = @"
                SELECT
                    j.jobID,
                    j.jobStatus,
                    c.clientName,
                    c.clientSurname
                FROM dbo.Job j
                INNER JOIN dbo.Quote q
                    ON j.quoteID = q.QuoteID
                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID
                INNER JOIN dbo.Client c
                    ON jr.clientID = c.clientID
                ORDER BY j.jobID DESC;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id =
                            Convert.ToInt32(reader["jobID"]);

                        string description =
                            "Job #" + id +
                            " - " +
                            Convert.ToString(reader["clientName"]) +
                            " " +
                            Convert.ToString(reader["clientSurname"]) +
                            " (" +
                            Convert.ToString(reader["jobStatus"]) +
                            ")";

                        ddlExistingJob.Items.Add(
                            new ListItem(
                                description,
                                id.ToString()));
                    }
                }
            }
        }

        // -------------------------------------------------
        // LOAD ALL JOBS GRID
        // -------------------------------------------------

        private void BindJobsGrid()
        {
            const string sql = @"
                SELECT
                    j.jobID,
                    CONCAT(
                        c.clientName, ' ',
                        c.clientSurname
                    ) AS ClientName,
                    jr.siteAddress,
                    j.startDate,
                    j.jobStatus,
                    j.totalFuelCost,
                    j.dumpingCost
                FROM dbo.Job j
                INNER JOIN dbo.Quote q
                    ON j.quoteID = q.QuoteID
                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID
                INNER JOIN dbo.Client c
                    ON jr.clientID = c.clientID
                WHERE
                    @search = ''
                    OR CAST(j.jobID AS VARCHAR(20))
                        LIKE @pattern
                    OR c.clientName LIKE @pattern
                    OR c.clientSurname LIKE @pattern
                    OR jr.siteAddress LIKE @pattern
                    OR j.jobStatus LIKE @pattern
                ORDER BY j.jobID DESC;";

            string search =
                (txtSearchJobs.Text ?? "").Trim();

            if (search.Length > 100)
                search = search.Substring(0, 100);

            DataTable table = new DataTable();

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@search", SqlDbType.VarChar, 100)
                    .Value = search;

                command.Parameters.Add(
                    "@pattern", SqlDbType.VarChar, 210)
                    .Value = "%" + search + "%";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
            }

            gvJobs.DataSource = table;
            gvJobs.DataBind();
        }

        protected void btnSearch_Click(
            object sender, EventArgs e)
        {
            try
            {
                gvJobs.PageIndex = 0;
                BindJobsGrid();
            }
            catch (Exception ex)
            {
                LogError("Search", ex);
                ShowError("Unable to search jobs.");
            }
        }

        protected void gvJobs_PageIndexChanging(
            object sender, GridViewPageEventArgs e)
        {
            gvJobs.PageIndex = e.NewPageIndex;

            try
            {
                BindJobsGrid();
            }
            catch (Exception ex)
            {
                LogError("Paging", ex);
                ShowError("Unable to load jobs.");
            }
        }

        protected void gvJobs_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            if (gvJobs.SelectedDataKey == null)
                return;

            int jobId = Convert.ToInt32(
                gvJobs.SelectedDataKey.Value);

            try
            {
                LoadExistingJob(jobId);
            }
            catch (Exception ex)
            {
                LogError("Select grid job", ex);
                ShowError("Unable to open the job.");
            }
        }

        // -------------------------------------------------
        // AUTOMATIC FUEL/TRAVEL COST CALCULATION
        // -------------------------------------------------

        private decimal CalculateFuelCost(
            SqlConnection connection,
            SqlTransaction transaction,
            int quoteId)
        {
            // Matches the formula in the Windows Forms
            // JobsForm: quote excluding VAT minus the
            // calculated service-item values.

            const string sql = @"
                SELECT
                    ROUND(
                        (q.amount / 1.15) -
                        SUM(
                            TRY_CONVERT(
                                DECIMAL(10,2),
                                id.detailValue
                            ) * jt.jobRate
                        ),
                        2
                    ) AS TravelFee
                FROM dbo.Quote q
                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID
                INNER JOIN dbo.RequestItem ri
                    ON jr.jobRequestID = ri.jobRequestID
                INNER JOIN dbo.JobType jt
                    ON ri.jobTypeID = jt.jobTypeID
                INNER JOIN dbo.ItemDetail id
                    ON ri.requestItemID = id.requestItemID
                WHERE q.QuoteID = @quote
                  AND
                  (
                    (jt.jobTypeName = 'Tree Felling'
                        AND id.jobDetailID = 1)
                    OR
                    (jt.jobTypeName = 'Grass Cutting'
                        AND id.jobDetailID = 6)
                    OR
                    (jt.jobTypeName = 'Tree Planting'
                        AND id.jobDetailID = 10)
                    OR
                    (jt.jobTypeName = 'Vegetation Clearance'
                        AND id.jobDetailID = 14)
                    OR
                    (jt.jobTypeName = 'Hedge Trimming'
                        AND id.jobDetailID = 18)
                  )
                GROUP BY q.amount;";

            object result = Scalar(
                connection,
                transaction,
                sql,
                IntParam("@quote", quoteId));

            if (result == null || result == DBNull.Value)
                return 0m;

            return Math.Round(
                Convert.ToDecimal(result),
                2,
                MidpointRounding.AwayFromZero);
        }

        // -------------------------------------------------
        // SELECT ACCEPTED QUOTATION
        // -------------------------------------------------




        protected void ddlAcceptedQuote_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            int quoteId;

            // Check that a quotation was selected.
            if (!int.TryParse(
                ddlAcceptedQuote.SelectedValue,
                out quoteId))
            {
                ClearFormFields();

                try
                {
                    LoadAvailableSlots(DateTime.Today, 0);
                }
                catch (Exception ex)
                {
                    LogError("Clear quotation selection", ex);
                }

                return;
            }

            try
            {
                string clientName = "";
                string siteAddress = "";
                decimal fuelCost = 0m;

                // Only allow quotations that:
                // 1. Have been Accepted
                // 2. Are not linked to another job
                // 3. Belong to a request that is not cancelled

                const string sql = @"
            SELECT
                q.QuoteID,
                q.jobRequestID,
                c.clientName,
                c.clientSurname,
                jr.siteAddress

            FROM dbo.Quote q

            INNER JOIN dbo.JobRequest jr
                ON q.jobRequestID = jr.jobRequestID

            INNER JOIN dbo.Client c
                ON jr.clientID = c.clientID

            WHERE q.QuoteID = @quote

              AND q.quoteStatus = 'Accepted'

              AND ISNULL(jr.status, '') <> 'Cancelled'

              AND NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.Job j
                  WHERE j.quoteID = q.QuoteID
              );";

                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        command.Parameters.Add(
                            "@quote", SqlDbType.Int)
                            .Value = quoteId;

                        using (SqlDataReader reader =
                            command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ClearFormFields();

                                ShowWarning(
                                    "This quotation is not available. " +
                                    "It must be accepted, not cancelled " +
                                    "and not already linked to a job.");

                                return;
                            }

                            clientName =
                                Convert.ToString(
                                    reader["clientName"]) +
                                " " +
                                Convert.ToString(
                                    reader["clientSurname"]);

                            siteAddress =
                                Convert.ToString(
                                    reader["siteAddress"]);
                        }
                    }

                    // Calculate fuel/travel cost
                    // using the existing desktop formula.
                    fuelCost = CalculateFuelCost(
                        connection,
                        null,
                        quoteId);
                }

                // Clear details from any previously
                // selected job or quotation.
                ClearFormFields();

                // Store the selected quotation ID
                // for the Create Job function.
                SelectedQuoteId = quoteId;
                SelectedJobId = 0;

                // Restore the selected quotation
                // because ClearFormFields resets it.
                ListItem selectedQuote =
                    ddlAcceptedQuote.Items.FindByValue(
                        quoteId.ToString());

                if (selectedQuote != null)
                {
                    ddlAcceptedQuote.SelectedValue =
                        quoteId.ToString();
                }

                // Fill job details automatically.
                txtJobID.Text = "";

                txtQuoteID.Text =
                    quoteId.ToString();

                txtClientName.Text =
                    clientName.Trim();

                txtSiteAddress.Text =
                    siteAddress;

                // Automatically calculated fuel cost.
                txtFuelCost.Text =
                    fuelCost.ToString(
                        "F2",
                        CultureInfo.InvariantCulture);

                // New jobs start with zero dumping cost.
                txtDumpingCost.Text = "0.00";

                // Set the default scheduled date.
                txtStartDate.Text =
                    DateTime.Today.ToString("yyyy-MM-dd");

                // New jobs start as Not Started.
                ddlJobStatus.SelectedValue = "Not Started";

                // Hide completion photo section
                // until the status is Completed.
                UpdateCompletionPanel();

                // Retrieve available scheduled
                // time slots for this date.
                LoadAvailableSlots(
                    DateTime.Today,
                    0);

                // Enable Create Job and disable
                // actions that need an existing job.
                UpdateButtonStates();

                ShowSuccess(
                    "Accepted quotation selected successfully. " +
                    "Choose a scheduled date and available " +
                    "time slot to create the job.");
            }
            catch (Exception ex)
            {
                LogError(
                    "Select accepted quotation",
                    ex);

                ClearFormFields();

                ShowError(
                    "Unable to load the selected quotation. " +
                    "Please check the quotation details " +
                    "and try again.");
            }
        }




        // -------------------------------------------------
        // SELECT EXISTING JOB
        // -------------------------------------------------

        protected void ddlExistingJob_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            int id;

            if (!int.TryParse(
                ddlExistingJob.SelectedValue,
                out id))
            {
                ClearFormFields();
                LoadAvailableSlots(DateTime.Today, 0);
                return;
            }

            try
            {
                LoadExistingJob(id);
            }
            catch (Exception ex)
            {
                LogError("Select existing job", ex);
                ShowError("Unable to load job details.");
            }
        }

        private void LoadExistingJob(int id)
        {
            const string sql = @"
                SELECT
                    j.jobID,
                    j.quoteID,
                    j.startDate,
                    j.jobStatus,
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
                WHERE j.jobID = @id;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(IntParam("@id", id));

                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        ShowWarning("Job not found.");
                        return;
                    }

                    ClearFormFields();

                    SelectedJobId = id;
                    SelectedQuoteId = 0;

                    txtJobID.Text = id.ToString();

                    txtQuoteID.Text =
                        Convert.ToString(reader["quoteID"]);

                    txtClientName.Text =
                        Convert.ToString(reader["clientName"]) +
                        " " +
                        Convert.ToString(reader["clientSurname"]);

                    txtSiteAddress.Text =
                        Convert.ToString(reader["siteAddress"]);

                    txtFuelCost.Text =
                        Convert.ToDecimal(
                            reader["totalFuelCost"])
                            .ToString(
                                "F2",
                                CultureInfo.InvariantCulture);

                    txtDumpingCost.Text =
                        Convert.ToDecimal(
                            reader["dumpingCost"])
                            .ToString(
                                "F2",
                                CultureInfo.InvariantCulture);

                    if (reader["startDate"] != DBNull.Value)
                    {
                        txtStartDate.Text =
                            Convert.ToDateTime(
                                reader["startDate"])
                                .ToString("yyyy-MM-dd");
                    }

                    string status =
                        Convert.ToString(reader["jobStatus"]);

                    if (status == "Archived" &&
                        ddlJobStatus.Items.FindByValue(
                            "Archived") == null)
                    {
                        ddlJobStatus.Items.Add(
                            new ListItem(
                                "Archived", "Archived"));
                    }

                    ListItem statusItem =
                        ddlJobStatus.Items.FindByValue(status);

                    if (statusItem != null)
                        ddlJobStatus.SelectedValue = status;
                }
            }

            ListItem existing =
                ddlExistingJob.Items.FindByValue(
                    id.ToString());

            if (existing != null)
                ddlExistingJob.SelectedValue = id.ToString();

            DateTime date;

            if (TryGetDate(out date))
            {
                LoadAvailableSlots(date, id);
            }

            UpdateCompletionPanel();
            UpdateButtonStates();
        }

        // -------------------------------------------------
        // DATE AND TIME SLOT LOADING
        // -------------------------------------------------

        private bool TryGetDate(out DateTime date)
        {
            return DateTime.TryParseExact(
                txtStartDate.Text,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        protected void txtStartDate_TextChanged(
            object sender, EventArgs e)
        {
            RefreshAvailableSlots();
        }

        protected void btnRefreshSlots_Click(
            object sender, EventArgs e)
        {
            RefreshAvailableSlots();
        }

        private void RefreshAvailableSlots()
        {
            DateTime date;

            if (!TryGetDate(out date))
            {
                ShowWarning("Please select a valid date.");
                return;
            }

            try
            {
                LoadAvailableSlots(date, SelectedJobId);
            }
            catch (Exception ex)
            {
                LogError("Load time slots", ex);
                ShowError("Unable to check time-slot availability.");
            }
        }

        private void LoadAvailableSlots(
            DateTime date, int currentJobId)
        {
            cblTimeSlots.Items.Clear();

            // Existing selected job's own slots remain
            // selectable. Other jobs' booked slots
            // are excluded.

            const string sql = @"
                SELECT
                    ts.timeSlotID,
                    ts.startTime,
                    ts.endTime,
                    CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.JobTimeSlot ownSlot
                        WHERE ownSlot.jobID = @job
                          AND ownSlot.timeSlotID = ts.timeSlotID
                    )
                    THEN 1 ELSE 0 END AS IsOwnSlot
                FROM dbo.TimeSlot ts
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.JobTimeSlot booked
                    INNER JOIN dbo.Job j
                        ON booked.jobID = j.jobID
                    WHERE j.startDate = @date
                      AND j.jobStatus <> 'Archived'
                      AND j.jobID <> @job
                      AND booked.timeSlotID = ts.timeSlotID
                )
                ORDER BY ts.startTime;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    DateParam("@date", date));

                command.Parameters.Add(
                    IntParam("@job", currentJobId));

                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(
                            reader["timeSlotID"]);

                        TimeSpan start =
                            (TimeSpan)reader["startTime"];

                        TimeSpan end =
                            (TimeSpan)reader["endTime"];

                        string caption =
                            start.ToString(@"hh\:mm") +
                            " - " +
                            end.ToString(@"hh\:mm");

                        ListItem item = new ListItem(
                            caption, id.ToString());

                        item.Selected =
                            Convert.ToInt32(
                                reader["IsOwnSlot"]) == 1;

                        cblTimeSlots.Items.Add(item);
                    }
                }
            }

            lblSlotMessage.Text =
                cblTimeSlots.Items.Count == 0
                ? "No time slots are available for this date."
                : "Choose one or more available time slots.";
        }

        private List<int> GetSelectedSlots()
        {
            List<int> selected = new List<int>();

            foreach (ListItem item in cblTimeSlots.Items)
            {
                int id;

                if (item.Selected &&
                    int.TryParse(item.Value, out id) &&
                    id > 0)
                {
                    if (!selected.Contains(id))
                        selected.Add(id);
                }
            }

            return selected;
        }

        // -------------------------------------------------
        // STATUS AND BUTTON HANDLING
        // -------------------------------------------------

        protected void ddlJobStatus_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            UpdateCompletionPanel();
        }

        private void UpdateCompletionPanel()
        {
            pnlCompletion.Visible =
                ddlJobStatus.SelectedValue == "Completed";
        }

        private void UpdateButtonStates()
        {
            bool newQuote =
                SelectedQuoteId > 0 &&
                SelectedJobId == 0;

            bool existing =
                SelectedJobId > 0;

            bool archived =
                ddlJobStatus.SelectedValue == "Archived";

            btnCreateJob.Enabled = newQuote;
            btnUpdateJob.Enabled = existing && !archived;
            btnArchiveJob.Enabled = existing && !archived;
            btnCancelJob.Enabled = existing;

            txtDumpingCost.ReadOnly = !existing;
        }

        private void ClearFormFields()
        {
            SelectedJobId = 0;
            SelectedQuoteId = 0;

            txtJobID.Text = "";
            txtQuoteID.Text = "";
            txtClientName.Text = "";
            txtSiteAddress.Text = "";

            txtFuelCost.Text = "0.00";
            txtDumpingCost.Text = "0.00";

            txtStartDate.Text =
                DateTime.Today.ToString("yyyy-MM-dd");

            ListItem archived =
                ddlJobStatus.Items.FindByValue("Archived");

            if (archived != null)
                ddlJobStatus.Items.Remove(archived);

            ddlJobStatus.SelectedValue = "Not Started";

            if (ddlAcceptedQuote.Items.Count > 0)
                ddlAcceptedQuote.SelectedIndex = 0;

            if (ddlExistingJob.Items.Count > 0)
                ddlExistingJob.SelectedIndex = 0;

            cblTimeSlots.Items.Clear();

            UpdateCompletionPanel();
            UpdateButtonStates();
        }

        protected void btnClear_Click(
            object sender, EventArgs e)
        {
            ClearFormFields();

            try
            {
                LoadAvailableSlots(
                    DateTime.Today, 0);
            }
            catch (Exception ex)
            {
                LogError("Clear", ex);
            }
        }

        // -------------------------------------------------
        // VALIDATION
        // -------------------------------------------------

        private bool ValidateJobInput(
            bool isCreate,
            out DateTime date,
            out List<int> slots,
            out string status,
            out decimal dumping)
        {
            date = DateTime.MinValue;
            slots = new List<int>();
            status = ddlJobStatus.SelectedValue;
            dumping = 0m;

            if (!TryGetDate(out date))
            {
                ShowWarning("Select a valid start date.");
                return false;
            }

            if (isCreate && date.Date < DateTime.Today)
            {
                ShowWarning(
                    "The job start date cannot be in the past.");
                return false;
            }

            if (status != "Not Started" &&
                status != "In Progress" &&
                status != "Completed")
            {
                ShowWarning("Select a valid job status.");
                return false;
            }

            slots = GetSelectedSlots();

            if (slots.Count == 0)
            {
                ShowWarning(
                    "Select at least one available time slot.");
                return false;
            }

            string costText =
                (txtDumpingCost.Text ?? "").Trim();

            if (string.IsNullOrEmpty(costText))
                costText = "0";

            if (!decimal.TryParse(
                costText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out dumping))
            {
                ShowWarning(
                    "Enter a valid dumping cost.");
                return false;
            }

            if (dumping < 0m ||
                dumping > 99999999.99m)
            {
                ShowWarning(
                    "Dumping cost must be between " +
                    "R0 and R99,999,999.99.");
                return false;
            }

            // Same as the original desktop form:
            // dumping cost begins at zero for new jobs.
            if (isCreate)
                dumping = 0m;

            return true;
        }

        // -------------------------------------------------
        // TRANSACTION-LEVEL SCHEDULE LOCK
        // -------------------------------------------------

        private void LockScheduleDate(
            SqlConnection connection,
            SqlTransaction transaction,
            DateTime date)
        {
            // Serialises concurrent bookings made through
            // this website for the same date.

            const string sql = @"
                DECLARE @result INT;

                EXEC @result = sys.sp_getapplock
                    @Resource = @resource,
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;

                SELECT @result;";

            string resource =
                "GiantGroupJobSchedule_" +
                date.ToString("yyyyMMdd");

            object result = Scalar(
                connection,
                transaction,
                sql,
                new SqlParameter(
                    "@resource", SqlDbType.NVarChar, 255)
                {
                    Value = resource
                });

            if (Convert.ToInt32(result) < 0)
            {
                throw new InvalidOperationException(
                    "Scheduling is temporarily busy. " +
                    "Please try again.");
            }
        }

        private void ValidateSlotsInDatabase(
            SqlConnection connection,
            SqlTransaction transaction,
            DateTime date,
            List<int> slots,
            int currentJobId)
        {
            foreach (int slotId in slots)
            {
                object exists = Scalar(
                    connection,
                    transaction,
                    @"SELECT COUNT(*)
                      FROM dbo.TimeSlot
                      WHERE timeSlotID = @slot;",
                    IntParam("@slot", slotId));

                if (Convert.ToInt32(exists) == 0)
                {
                    throw new InvalidOperationException(
                        "An invalid time slot was selected.");
                }

                object booked = Scalar(
                    connection,
                    transaction,
                    @"
                    SELECT COUNT(*)
                    FROM dbo.JobTimeSlot jts
                    INNER JOIN dbo.Job j
                        ON jts.jobID = j.jobID
                    WHERE j.startDate = @date
                      AND jts.timeSlotID = @slot
                      AND j.jobID <> @job
                      AND j.jobStatus <> 'Archived';",
                    DateParam("@date", date),
                    IntParam("@slot", slotId),
                    IntParam("@job", currentJobId));

                if (Convert.ToInt32(booked) > 0)
                {
                    throw new InvalidOperationException(
                        "One or more selected time slots " +
                        "have already been booked.");
                }
            }
        }

        private void ReplaceJobSlots(
            SqlConnection connection,
            SqlTransaction transaction,
            int jobId,
            List<int> slots)
        {
            Execute(
                connection,
                transaction,
                @"DELETE FROM dbo.JobTimeSlot
                  WHERE jobID = @job;",
                IntParam("@job", jobId));

            foreach (int slotId in slots)
            {
                Execute(
                    connection,
                    transaction,
                    @"INSERT INTO dbo.JobTimeSlot
                        (jobID, timeSlotID)
                      VALUES (@job, @slot);",
                    IntParam("@job", jobId),
                    IntParam("@slot", slotId));
            }
        }

        // -------------------------------------------------
        // COMPLETION PHOTO HELPERS
        // -------------------------------------------------

        private class PhotoData
        {
            public byte[] Bytes;
            public string Extension;
        }

        private PhotoData ReadCompletionPhoto()
        {
            if (!fuCompletionPhoto.HasFile)
                return null;

            HttpPostedFile file =
                fuCompletionPhoto.PostedFile;

            if (file == null ||
                file.ContentLength <= 0 ||
                file.ContentLength > MaxPhotoBytes)
            {
                throw new InvalidOperationException(
                    "The completion photo must be " +
                    "15 MB or smaller.");
            }

            byte[] bytes = fuCompletionPhoto.FileBytes;

            string extension = null;

            if (bytes.Length >= 3 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8 &&
                bytes[2] == 0xFF)
            {
                extension = ".jpg";
            }
            else if (bytes.Length >= 8 &&
                     bytes[0] == 0x89 &&
                     bytes[1] == 0x50 &&
                     bytes[2] == 0x4E &&
                     bytes[3] == 0x47 &&
                     bytes[4] == 0x0D &&
                     bytes[5] == 0x0A &&
                     bytes[6] == 0x1A &&
                     bytes[7] == 0x0A)
            {
                extension = ".png";
            }

            if (extension == null)
            {
                throw new InvalidOperationException(
                    "Only JPEG and PNG completion " +
                    "photos are allowed.");
            }

            return new PhotoData
            {
                Bytes = bytes,
                Extension = extension
            };
        }

        private bool HasCompletionPhoto(
            SqlConnection connection,
            SqlTransaction transaction,
            int jobId)
        {
            object count = Scalar(
                connection,
                transaction,
                @"SELECT COUNT(*)
                  FROM dbo.SitePhoto
                  WHERE jobID = @job
                    AND photoType = 'AFTER';",
                IntParam("@job", jobId));

            return Convert.ToInt32(count) > 0;
        }

        private string SaveCompletionPhoto(
            SqlConnection connection,
            SqlTransaction transaction,
            int jobId,
            int requestId,
            PhotoData photo)
        {
            if (photo == null)
                return null;

            // Uses the same storage structure as
            // the existing MobileApi photo upload.

            string relativeDirectory =
                "~/App_Data/SitePhotos/JobRequest_" +
                requestId;

            string physicalDirectory =
                Server.MapPath(relativeDirectory);

            Directory.CreateDirectory(physicalDirectory);

            string fileName =
                "AFTER_" +
                DateTime.UtcNow.ToString(
                    "yyyyMMdd_HHmmss") +
                "_" +
                Guid.NewGuid().ToString("N") +
                photo.Extension;

            string relativePath =
                relativeDirectory + "/" + fileName;

            string physicalPath =
                Path.Combine(
                    physicalDirectory, fileName);

            File.WriteAllBytes(
                physicalPath, photo.Bytes);

            try
            {
                Execute(
                    connection,
                    transaction,
                    @"INSERT INTO dbo.SitePhoto
                      (
                          photoType,
                          filePath,
                          uploadDate,
                          jobID,
                          jobRequestID
                      )
                      VALUES
                      (
                          'AFTER',
                          @path,
                          GETDATE(),
                          @job,
                          @request
                      );",
                    new SqlParameter(
                        "@path", SqlDbType.VarChar, 255)
                    {
                        Value = relativePath
                    },
                    IntParam("@job", jobId),
                    IntParam("@request", requestId));
            }
            catch
            {
                DeletePhysicalPhoto(physicalPath);
                throw;
            }

            return physicalPath;
        }

        private void DeletePhysicalPhoto(string physicalPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(physicalPath) &&
                    File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                LogError("Delete photo", ex);
            }
        }

        private void DeleteStoredPhoto(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
                return;

            // Never delete arbitrary paths provided
            // by database records.

            if (!storedPath.StartsWith(
                    "~/App_Data/SitePhotos/JobRequest_",
                    StringComparison.OrdinalIgnoreCase))
                return;

            if (storedPath.Contains(".."))
                return;

            try
            {
                string root = Path.GetFullPath(
                    Server.MapPath("~/App_Data/SitePhotos"));

                string physical = Path.GetFullPath(
                    Server.MapPath(storedPath));

                string prefix =
                    root.TrimEnd(
                        Path.DirectorySeparatorChar) +
                    Path.DirectorySeparatorChar;

                if (physical.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    DeletePhysicalPhoto(physical);
                }
            }
            catch (Exception ex)
            {
                LogError("Delete stored photo", ex);
            }
        }

        // -------------------------------------------------
        // CREATE JOB
        // -------------------------------------------------


        protected void btnCreateJob_Click(
            object sender, EventArgs e)
        {
            // A sent quotation must be selected first.
            if (SelectedQuoteId <= 0 ||
                SelectedJobId != 0)
            {
                ShowWarning(
                    "Please select a valid sent quotation first.");

                return;
            }

            DateTime date;
            List<int> slots;
            string status;
            decimal dumping;

            // Validate the selected date, job status,
            // time slots and dumping cost.
            if (!ValidateJobInput(
                true,
                out date,
                out slots,
                out status,
                out dumping))
            {
                return;
            }

            PhotoData photo;

            try
            {
                photo = ReadCompletionPhoto();

                if (status == "Completed" &&
                    photo == null)
                {
                    ShowWarning(
                        "A completion photo is required " +
                        "before a job can be marked Completed.");

                    return;
                }
            }
            catch (Exception ex)
            {
                ShowWarning(ex.Message);
                return;
            }

            string newPhotoPath = null;
            bool transactionCommitted = false;
            int createdJobId = 0;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction =
                        connection.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        try
                        {
                            // Prevent simultaneous scheduling
                            // requests from booking the same date.
                            LockScheduleDate(
                                connection,
                                transaction,
                                date);

                            // Recheck the quotation before saving.
                            //
                            // Rules:
                            // - Quote must be Sent
                            // - Quote must not be expired
                            // - Request must not be cancelled
                            // - Quote cannot already have a job

                            const string quoteSql = @"
                        SELECT q.jobRequestID

                        FROM dbo.Quote q
                            WITH (UPDLOCK, HOLDLOCK)

                        INNER JOIN dbo.JobRequest jr
                            ON q.jobRequestID =
                               jr.jobRequestID

                        WHERE q.QuoteID = @quote

                          AND q.quoteStatus = 'Sent'

                          AND q.expiryDate >=
                              CAST(GETDATE() AS DATE)

                          AND ISNULL(jr.status, '')
                              <> 'Cancelled'

                          AND NOT EXISTS
                          (
                              SELECT 1
                              FROM dbo.Job j
                                  WITH (UPDLOCK, HOLDLOCK)
                              WHERE j.quoteID = q.QuoteID
                          );";

                            object requestObject = Scalar(
                                connection,
                                transaction,
                                quoteSql,
                                IntParam(
                                    "@quote",
                                    SelectedQuoteId));

                            if (requestObject == null ||
                                requestObject == DBNull.Value)
                            {
                                throw new InvalidOperationException(
                                    "This quotation is no longer " +
                                    "eligible for scheduling. " +
                                    "It must be Sent, unexpired, " +
                                    "not cancelled and not " +
                                    "linked to another job.");
                            }

                            int requestId =
                                Convert.ToInt32(requestObject);

                            // Recheck availability inside the
                            // same database transaction.
                            ValidateSlotsInDatabase(
                                connection,
                                transaction,
                                date,
                                slots,
                                0);

                            // Use the existing automatic
                            // fuel/travel calculation.
                            decimal fuel = CalculateFuelCost(
                                connection,
                                transaction,
                                SelectedQuoteId);

                            if (fuel < 0m ||
                                fuel > 99999999.99m)
                            {
                                throw new InvalidOperationException(
                                    "The calculated fuel cost " +
                                    "is outside the permitted range.");
                            }

                            // Create the new job.
                            const string insertSql = @"
                        INSERT INTO dbo.Job
                        (
                            startDate,
                            jobStatus,
                            totalFuelCost,
                            dumpingCost,
                            quoteID
                        )
                        VALUES
                        (
                            @date,
                            @status,
                            @fuel,
                            @dumping,
                            @quote
                        );

                        SELECT CAST(
                            SCOPE_IDENTITY() AS INT);";

                            object result = Scalar(
                                connection,
                                transaction,
                                insertSql,

                                DateParam("@date", date),

                                new SqlParameter(
                                    "@status",
                                    SqlDbType.VarChar, 30)
                                {
                                    Value = status
                                },

                                MoneyParam("@fuel", fuel),

                                MoneyParam("@dumping", dumping),

                                IntParam(
                                    "@quote",
                                    SelectedQuoteId));

                            if (result == null ||
                                result == DBNull.Value)
                            {
                                throw new InvalidOperationException(
                                    "The job could not be created.");
                            }

                            createdJobId =
                                Convert.ToInt32(result);

                            // Save the scheduled time slots.
                            ReplaceJobSlots(
                                connection,
                                transaction,
                                createdJobId,
                                slots);

                            // Save a completion photo if one
                            // was supplied for a Completed job.
                            if (photo != null)
                            {
                                newPhotoPath =
                                    SaveCompletionPhoto(
                                        connection,
                                        transaction,
                                        createdJobId,
                                        requestId,
                                        photo);
                            }

                            // Save all changes together.
                            transaction.Commit();
                            transactionCommitted = true;
                        }
                        catch
                        {
                            if (!transactionCommitted)
                            {
                                try
                                {
                                    transaction.Rollback();
                                }
                                catch (Exception rollbackEx)
                                {
                                    LogError(
                                        "Create job rollback",
                                        rollbackEx);
                                }

                                DeletePhysicalPhoto(newPhotoPath);
                            }

                            throw;
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                ShowWarning(ex.Message);
                return;
            }
            catch (SqlException ex)
            {
                LogError("Create job SQL", ex);

                ShowError(
                    "Unable to create the job. " +
                    "The quotation may already be linked " +
                    "to another job, or the selected " +
                    "time slots may be unavailable.");

                return;
            }
            catch (Exception ex)
            {
                LogError("Create job", ex);

                ShowError(
                    "Unable to create the job. " +
                    "Please check the details and try again.");

                return;
            }

            // The job has been saved successfully.
            // Refresh the dropdowns and job records.
            try
            {
                BindAcceptedQuotes();
                BindExistingJobs();
                BindJobsGrid();

                LoadExistingJob(createdJobId);

                ShowSuccess(
                    "Job created successfully! " +
                    "Job ID: " + createdJobId);
            }
            catch (Exception ex)
            {
                LogError(
                    "Refresh after creating job",
                    ex);

                ShowWarning(
                    "Job #" + createdJobId +
                    " was saved successfully, but the " +
                    "page could not refresh. " +
                    "Reload the page to view the job.");
            }
        }


        // -------------------------------------------------
        // UPDATE JOB
        // -------------------------------------------------

        protected void btnUpdateJob_Click(
            object sender, EventArgs e)
        {
            if (SelectedJobId <= 0)
            {
                ShowWarning(
                    "Select an existing job first.");
                return;
            }

            DateTime date;
            List<int> slots;
            string status;
            decimal dumping;

            if (!ValidateJobInput(
                false,
                out date,
                out slots,
                out status,
                out dumping))
                return;

            PhotoData photo;

            try
            {
                photo = ReadCompletionPhoto();
            }
            catch (Exception ex)
            {
                ShowWarning(ex.Message);
                return;
            }

            string newPhotoPath = null;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction =
                        connection.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        try
                        {
                            LockScheduleDate(
                                connection,
                                transaction,
                                date);

                            const string currentSql = @"
                                SELECT
                                    j.startDate,
                                    j.jobStatus,
                                    q.jobRequestID
                                FROM dbo.Job j
                                    WITH (UPDLOCK, HOLDLOCK)
                                INNER JOIN dbo.Quote q
                                    ON j.quoteID = q.QuoteID
                                WHERE j.jobID = @job;";

                            DateTime oldDate;
                            string oldStatus;
                            int requestId;

                            using (SqlCommand command =
                                new SqlCommand(
                                    currentSql,
                                    connection,
                                    transaction))
                            {
                                command.Parameters.Add(
                                    IntParam(
                                        "@job",
                                        SelectedJobId));

                                using (SqlDataReader reader =
                                    command.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new InvalidOperationException(
                                            "Job not found.");
                                    }

                                    oldDate =
                                        reader["startDate"] ==
                                        DBNull.Value
                                        ? DateTime.MinValue
                                        : Convert.ToDateTime(
                                            reader["startDate"]);

                                    oldStatus =
                                        Convert.ToString(
                                            reader["jobStatus"]);

                                    requestId =
                                        Convert.ToInt32(
                                            reader["jobRequestID"]);
                                }
                            }

                            if (oldStatus == "Archived")
                            {
                                throw new InvalidOperationException(
                                    "Archived jobs cannot be edited.");
                            }

                            List<int> oldSlots =
                                new List<int>();

                            using (SqlCommand command =
                                new SqlCommand(
                                    @"SELECT timeSlotID
                                      FROM dbo.JobTimeSlot
                                      WHERE jobID = @job;",
                                    connection,
                                    transaction))
                            {
                                command.Parameters.Add(
                                    IntParam(
                                        "@job",
                                        SelectedJobId));

                                using (SqlDataReader reader =
                                    command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        oldSlots.Add(
                                            reader.GetInt32(0));
                                    }
                                }
                            }

                            bool scheduleChanged =
                                oldDate.Date != date.Date ||
                                !new HashSet<int>(oldSlots)
                                    .SetEquals(slots);

                            if (scheduleChanged)
                            {
                                if (date.Date < DateTime.Today)
                                {
                                    throw new InvalidOperationException(
                                        "Cannot reschedule a job " +
                                        "to a past date.");
                                }

                                // Changing the schedule after
                                // resources are assigned could
                                // leave staff/asset slots wrong.
                                object assignments = Scalar(
                                    connection,
                                    transaction,
                                    @"
                                    SELECT
                                      (SELECT COUNT(*)
                                       FROM dbo.JobStaffAssignment
                                       WHERE jobID = @job)
                                      +
                                      (SELECT COUNT(*)
                                       FROM dbo.JobAssetAssignment
                                       WHERE jobID = @job);",
                                    IntParam(
                                        "@job",
                                        SelectedJobId));

                                if (Convert.ToInt32(assignments) > 0)
                                {
                                    throw new InvalidOperationException(
                                        "This job already has staff " +
                                        "or equipment assigned. " +
                                        "Remove or coordinate those " +
                                        "allocations before rescheduling.");
                                }
                            }

                            ValidateSlotsInDatabase(
                                connection,
                                transaction,
                                date,
                                slots,
                                SelectedJobId);

                            if (status == "Completed" &&
                                photo == null &&
                                !HasCompletionPhoto(
                                    connection,
                                    transaction,
                                    SelectedJobId))
                            {
                                throw new InvalidOperationException(
                                    "Upload a completion photo " +
                                    "before marking this job Completed.");
                            }

                            Execute(
                                connection,
                                transaction,
                                @"
                                UPDATE dbo.Job
                                SET
                                    startDate = @date,
                                    jobStatus = @status,
                                    dumpingCost = @dumping
                                WHERE jobID = @job;",
                                DateParam("@date", date),
                                new SqlParameter(
                                    "@status",
                                    SqlDbType.VarChar, 30)
                                {
                                    Value = status
                                },
                                MoneyParam(
                                    "@dumping", dumping),
                                IntParam(
                                    "@job", SelectedJobId));

                            if (scheduleChanged)
                            {
                                ReplaceJobSlots(
                                    connection,
                                    transaction,
                                    SelectedJobId,
                                    slots);
                            }

                            if (photo != null)
                            {
                                if (status != "Completed")
                                {
                                    throw new InvalidOperationException(
                                        "Completion photos can only " +
                                        "be uploaded for completed jobs.");
                                }

                                newPhotoPath =
                                    SaveCompletionPhoto(
                                        connection,
                                        transaction,
                                        SelectedJobId,
                                        requestId,
                                        photo);
                            }

                            if (status == "Completed")
                            {
                                ReleaseOwnedAssets(
                                    connection,
                                    transaction,
                                    SelectedJobId);
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            DeletePhysicalPhoto(newPhotoPath);
                            throw;
                        }
                    }
                }

                int savedJobId = SelectedJobId;

                BindExistingJobs();
                BindJobsGrid();
                LoadExistingJob(savedJobId);

                ShowSuccess(
                    "Job #" + savedJobId +
                    " updated successfully.");
            }
            catch (InvalidOperationException ex)
            {
                ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                LogError("Update job", ex);
                ShowError(
                    "Unable to update the job.");
            }
        }

        // -------------------------------------------------
        // RELEASE OWNED ASSETS AFTER COMPLETION
        // -------------------------------------------------

        private void ReleaseOwnedAssets(
            SqlConnection connection,
            SqlTransaction transaction,
            int jobId)
        {
            // Staff.status = Active describes an active
            // account, not free/busy availability.
            // We preserve staff assignments as history.
            //
            // Availability is determined from the
            // schedule and non-completed jobs.
            //
            // Owned assets are released only when
            // no other active job uses them.

            Execute(
                connection,
                transaction,
                @"
                UPDATE asset
                SET asset.assetStatus = 'Available'
                FROM dbo.OwnedAsset asset
                WHERE asset.assetStatus = 'In Use'
                  AND EXISTS
                  (
                      SELECT 1
                      FROM dbo.JobAssetAssignment ja
                      WHERE ja.ownedAssetID = asset.assetID
                        AND ja.jobID = @job
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.JobAssetAssignment otherJa
                      INNER JOIN dbo.Job otherJob
                        ON otherJa.jobID = otherJob.jobID
                      WHERE otherJa.ownedAssetID =
                            asset.assetID
                        AND otherJa.jobID <> @job
                        AND otherJob.jobStatus IN
                            ('Not Started', 'In Progress')
                  );",
                IntParam("@job", jobId));
        }

        // -------------------------------------------------
        // ARCHIVE JOB
        // -------------------------------------------------

        protected void btnArchiveJob_Click(
            object sender, EventArgs e)
        {
            if (SelectedJobId <= 0)
            {
                ShowWarning(
                    "Select a job to archive.");
                return;
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction =
                        connection.BeginTransaction())
                    {
                        try
                        {
                            int rows = Execute(
                                connection,
                                transaction,
                                @"
                                UPDATE dbo.Job
                                SET jobStatus = 'Archived'
                                WHERE jobID = @job
                                  AND jobStatus <> 'Archived';",
                                IntParam(
                                    "@job",
                                    SelectedJobId));

                            if (rows == 0)
                            {
                                throw new InvalidOperationException(
                                    "Job not found or already archived.");
                            }

                            ReleaseOwnedAssets(
                                connection,
                                transaction,
                                SelectedJobId);

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                int id = SelectedJobId;

                BindExistingJobs();
                BindJobsGrid();
                LoadExistingJob(id);

                ShowSuccess(
                    "Job #" + id +
                    " archived successfully.");
            }
            catch (InvalidOperationException ex)
            {
                ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                LogError("Archive", ex);
                ShowError("Unable to archive the job.");
            }
        }

        // -------------------------------------------------
        // PERMANENT CANCELLATION
        // -------------------------------------------------

        protected void btnCancelJob_Click(
            object sender, EventArgs e)
        {
            if (SelectedJobId <= 0)
            {
                ShowWarning(
                    "Select a job to cancel.");
                return;
            }

            int jobId = SelectedJobId;

            List<string> photoPaths =
                new List<string>();

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction =
                        connection.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        try
                        {
                            object requestObj = Scalar(
                                connection,
                                transaction,
                                @"
                                SELECT q.jobRequestID
                                FROM dbo.Job j
                                    WITH (UPDLOCK, HOLDLOCK)
                                INNER JOIN dbo.Quote q
                                    ON j.quoteID = q.QuoteID
                                WHERE j.jobID = @job;",
                                IntParam("@job", jobId));

                            if (requestObj == null)
                            {
                                throw new InvalidOperationException(
                                    "The selected job does not exist.");
                            }

                            int requestId =
                                Convert.ToInt32(requestObj);

                            // IMPORTANT:
                            // The Payment FK uses ON DELETE
                            // CASCADE. Deleting a paid job
                            // could erase transaction history.
                            // Therefore paid jobs are blocked.

                            object payments = Scalar(
                                connection,
                                transaction,
                                @"
                                SELECT COUNT(*)
                                FROM dbo.Payment
                                    WITH (UPDLOCK, HOLDLOCK)
                                WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            if (Convert.ToInt32(payments) > 0)
                            {
                                throw new InvalidOperationException(
                                    "This job has payment records. " +
                                    "Cancellation is blocked to " +
                                    "protect financial history.");
                            }

                            // Prevent deleting a job while
                            // a payment is pending or paid
                            // through PayFast.

                            object intents = Scalar(
                                connection,
                                transaction,
                                @"
                                SELECT COUNT(*)
                                FROM dbo.PayFastPaymentIntent
                                    WITH (UPDLOCK, HOLDLOCK)
                                WHERE JobID = @job
                                  AND PaymentStatus IN
                                      ('Pending', 'Paid');",
                                IntParam("@job", jobId));

                            if (Convert.ToInt32(intents) > 0)
                            {
                                throw new InvalidOperationException(
                                    "This job has an active or " +
                                    "completed PayFast transaction. " +
                                    "Resolve the payment first.");
                            }

                            // Collect job photo paths before
                            // deleting the database rows.

                            using (SqlCommand command =
                                new SqlCommand(
                                    @"SELECT filePath
                                      FROM dbo.SitePhoto
                                      WHERE jobID = @job;",
                                    connection,
                                    transaction))
                            {
                                command.Parameters.Add(
                                    IntParam("@job", jobId));

                                using (SqlDataReader reader =
                                    command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        photoPaths.Add(
                                            reader.GetString(0));
                                    }
                                }
                            }

                            // Release owned assets before
                            // removing the assignments.

                            ReleaseOwnedAssets(
                                connection,
                                transaction,
                                jobId);

                            // Delete job photos.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM dbo.SitePhoto
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            // Delete staff booking slots.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM
                                    dbo.JobStaffAssignmentSlot
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            // Delete staff assignments.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM
                                    dbo.JobStaffAssignment
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            // Delete asset booking slots.
                            Execute(
                                connection,
                                transaction,
                                @"
                                DELETE slot
                                FROM dbo.JobAssetAssignmentSlot slot
                                INNER JOIN dbo.JobAssetAssignment ja
                                    ON slot.assignmentID =
                                       ja.AssignmentID
                                WHERE ja.jobID = @job;",
                                IntParam("@job", jobId));

                            // Delete asset assignments.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM
                                    dbo.JobAssetAssignment
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            // Delete hired-asset usage.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM dbo.HiredAssetUsage
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            // Delete job time slots.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM dbo.JobTimeSlot
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            // Remove failed/cancelled
                            // payment checkout intents only.
                            Execute(
                                connection,
                                transaction,
                                @"DELETE FROM
                                    dbo.PayFastPaymentIntent
                                  WHERE JobID = @job
                                    AND PaymentStatus NOT IN
                                        ('Pending', 'Paid');",
                                IntParam("@job", jobId));

                            // Delete the job itself.
                            int deleted = Execute(
                                connection,
                                transaction,
                                @"DELETE FROM dbo.Job
                                  WHERE jobID = @job;",
                                IntParam("@job", jobId));

                            if (deleted != 1)
                            {
                                throw new InvalidOperationException(
                                    "Job cancellation failed.");
                            }

                            // Retain quote/request history,
                            // but mark the request Cancelled
                            // so it cannot be rescheduled.

                            Execute(
                                connection,
                                transaction,
                                @"UPDATE dbo.JobRequest
                                  SET status = 'Cancelled'
                                  WHERE jobRequestID = @request;",
                                IntParam(
                                    "@request", requestId));

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                // Physical files are removed only after
                // the database transaction commits.

                foreach (string path in photoPaths)
                {
                    DeleteStoredPhoto(path);
                }

                BindAcceptedQuotes();
                BindExistingJobs();

                gvJobs.PageIndex = 0;
                BindJobsGrid();

                ClearFormFields();
                LoadAvailableSlots(
                    DateTime.Today, 0);

                ShowSuccess(
                    "Job #" + jobId +
                    " cancelled and its operational " +
                    "records removed successfully.");
            }
            catch (InvalidOperationException ex)
            {
                ShowWarning(ex.Message);
            }
            catch (Exception ex)
            {
                LogError("Cancel job", ex);
                ShowError(
                    "Unable to cancel the job. " +
                    "No changes should be assumed; " +
                    "check the job record.");
            }
        }
    }
}
