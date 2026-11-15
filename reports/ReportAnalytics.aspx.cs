
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ReportAnalytics : Page
    {
        private const string EmptyJson =
            "{\"labels\":[],\"values\":[]}";

        // Use your existing GroupWst1 connection.
        private string Conn
        {
            get
            {
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings[
                        "DefaultConnection"];

                if (settings == null)
                    throw new ConfigurationErrorsException(
                        "DefaultConnection was not found.");

                return settings.ConnectionString;
            }
        }

        // ==================================================
        // OWNER SECURITY AND PAGE LOAD
        // ==================================================

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster siteMaster = Master as SiteMaster;

            if (siteMaster == null ||
                !siteMaster.CanSeeReports())
            {
                throw new HttpException(
                    403,
                    "Only the owner can view report analytics.");
            }

            if (!IsPostBack)
            {
                txtStartDate.Text =
                    DateTime.Today.AddMonths(-6)
                    .ToString("yyyy-MM-dd");

                txtEndDate.Text =
                    DateTime.Today.ToString("yyyy-MM-dd");

                LoadClientTypes();
                LoadAnalytics();
            }
        }

        protected void btnLoad_Click(
            object sender, EventArgs e)
        {
            LoadAnalytics();
        }

        // ==================================================
        // LOAD CLIENT TYPES FOR FILTER
        // ==================================================

        private void LoadClientTypes()
        {
            ddlClientType.Items.Clear();

            ddlClientType.Items.Add(
                new ListItem("All clients", "All"));

            using (SqlConnection cn =
                new SqlConnection(Conn))
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT DISTINCT clientType
                  FROM dbo.Client
                  WHERE clientType IS NOT NULL
                    AND LTRIM(RTRIM(clientType)) <> ''
                  ORDER BY clientType", cn))
            {
                cn.Open();

                using (SqlDataReader reader =
                    cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string type =
                            Convert.ToString(reader[0]);

                        ddlClientType.Items.Add(
                            new ListItem(type, type));
                    }
                }
            }
        }

        // ==================================================
        // MAIN LOAD METHOD
        // ==================================================

        private void LoadAnalytics()
        {
            ResetChartData();
            lblMessage.Text = "";

            DateTime start;
            DateTime end;

            if (!DateTime.TryParseExact(
                    txtStartDate.Text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out start) ||
                !DateTime.TryParseExact(
                    txtEndDate.Text,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out end))
            {
                lblMessage.Text =
                    "Please select valid start and end dates.";
                return;
            }

            if (start.Date > end.Date)
            {
                lblMessage.Text =
                    "Start date cannot be after end date.";
                return;
            }

            // Keep the report range reasonable.
            if ((end.Date - start.Date).TotalDays > 3660)
            {
                lblMessage.Text =
                    "Please select a date range of ten years or less.";
                return;
            }

            try
            {
                string clientType = ddlClientType.SelectedValue;

                LoadIncome(start, end);
                LoadExpenses(start, end);
                LoadProfits(start, end);
                LoadServiceProfitability(start, end);
                LoadQuoteConversion(start, end);
                LoadClientValue(start, end, clientType);
                LoadPopularAreas(start, end);
                LoadWebsiteImpact(start, end);
                LoadOutstandingPayments();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Report Analytics: " + ex);

                // Never send SQL details or connection
                // information to the browser.
                ResetChartData();

                lblMessage.Text =
                    "Unable to load one or more reports. " +
                    "Please check the server logs.";
            }
        }

        private void LoadOutstandingPayments()
        {
            // Retrieves all current outstanding balances.
            // The date filter is deliberately not applied:
            // older debts must remain visible.

            string sql = @"
        WITH PaymentTotals AS
        (
            SELECT
                jobID,
                SUM(
                    CONVERT(decimal(19,2), amountPaid)
                ) AS TotalPaid
            FROM dbo.Payment
            GROUP BY jobID
        ),
        JobBalances AS
        (
            SELECT
                j.jobID AS JobID,
                j.startDate AS JobStartDate,
                j.jobStatus AS JobStatus,

                jr.jobRequestID AS RequestID,
                jr.siteAddress AS SiteAddress,

                c.clientName + ' ' + c.clientSurname
                    AS ClientName,

                CONVERT(decimal(19,2), q.amount)
                    AS QuotedAmount,

                ISNULL(p.TotalPaid, 0)
                    AS TotalPaid,

                CONVERT(decimal(19,2), q.amount)
                    - ISNULL(p.TotalPaid, 0)
                    AS OutstandingBalance

            FROM dbo.Job j

            INNER JOIN dbo.Quote q
                ON j.quoteID = q.QuoteID

            INNER JOIN dbo.JobRequest jr
                ON q.jobRequestID = jr.jobRequestID

            INNER JOIN dbo.Client c
                ON jr.clientID = c.clientID

            LEFT JOIN PaymentTotals p
                ON j.jobID = p.jobID
        )

        SELECT
            JobID,
            JobStartDate,
            JobStatus,
            RequestID,
            SiteAddress,
            ClientName,
            QuotedAmount,
            TotalPaid,
            OutstandingBalance,

            CASE
                WHEN TotalPaid > 0
                THEN 'Partially Paid'
                ELSE 'Unpaid'
            END AS PaymentStatus

        FROM JobBalances

        WHERE OutstandingBalance > 0

        ORDER BY
            OutstandingBalance DESC,
            JobID;";

            // The reusable Query method adds date parameters,
            // but this particular SQL does not use them.
            DataTable dt = Query(
                sql,
                DateTime.Today,
                DateTime.Today
            );

            List<object> rows = new List<object>();

            List<string> chartLabels = new List<string>();
            List<decimal> chartValues = new List<decimal>();

            decimal totalOutstanding = 0m;
            int partialCount = 0;

            foreach (DataRow row in dt.Rows)
            {
                int jobId = Convert.ToInt32(row["JobID"]);

                string clientName =
                    Convert.ToString(row["ClientName"]);

                string siteAddress =
                    Convert.ToString(row["SiteAddress"]);

                string jobStatus =
                    Convert.ToString(row["JobStatus"]);

                string paymentStatus =
                    Convert.ToString(row["PaymentStatus"]);

                decimal quoted =
                    Convert.ToDecimal(row["QuotedAmount"]);

                decimal paid =
                    Convert.ToDecimal(row["TotalPaid"]);

                decimal balance =
                    Convert.ToDecimal(row["OutstandingBalance"]);

                totalOutstanding += balance;

                if (paid > 0)
                    partialCount++;

                // Keep all records for the detailed table.
                rows.Add(new
                {
                    jobId = jobId,
                    client = clientName,
                    site = siteAddress,
                    jobStatus = jobStatus,
                    quoted = quoted,
                    paid = paid,
                    balance = balance,
                    paymentStatus = paymentStatus
                });

                // Show only the 10 largest balances in the graph.
                if (chartLabels.Count < 10)
                {
                    chartLabels.Add(
                        "Job #" + jobId + " | " + clientName
                    );

                    chartValues.Add(balance);
                }
            }

            // Reuse the Json() helper already present
            // in your existing ReportAnalytics.aspx.cs.
            hfOutstandingData.Value = Json(new
            {
                labels = chartLabels,
                values = chartValues,
                rows = rows,

                total = totalOutstanding,
                count = dt.Rows.Count,
                partialCount = partialCount
            });
        }


        private void ResetChartData()
        {
            hfIncomeData.Value = EmptyJson;
            hfExpenseData.Value = EmptyJson;
            hfProfitData.Value = "{}";
            hfServiceData.Value = EmptyJson;
            hfQuoteData.Value = EmptyJson;
            hfClientData.Value = EmptyJson;
            hfAreaData.Value = "{}";
            hfWebsiteData.Value = "{}";
            hfOutstandingData.Value = "{}";
        }

        // ==================================================
        // REUSABLE SQL QUERY
        // ==================================================

        private DataTable Query(
            string sql,
            DateTime start,
            DateTime end,
            string clientType = "All")
        {
            DataTable dt = new DataTable();

            using (SqlConnection cn =
                new SqlConnection(Conn))
            using (SqlCommand cmd =
                new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add(
                    "@StartDate", SqlDbType.Date)
                    .Value = start.Date;

                cmd.Parameters.Add(
                    "@EndDate", SqlDbType.Date)
                    .Value = end.Date;

                // Used only by Client Value Analysis.
                if (sql.Contains("@ClientType"))
                {
                    cmd.Parameters.Add(
                        "@ClientType", SqlDbType.VarChar, 100)
                        .Value = clientType ?? "All";
                }

                using (SqlDataAdapter da =
                    new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return dt;
        }

        // ==================================================
        // JSON HELPERS
        // ==================================================

        private string Json(object value)
        {
            return new JavaScriptSerializer()
                .Serialize(value);
        }

        private string ChartJson(
            List<string> labels,
            List<decimal> values)
        {
            return Json(new
            {
                labels = labels,
                values = values
            });
        }

        private decimal Money(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) ||
                row[column] == DBNull.Value)
                return 0m;

            return Convert.ToDecimal(row[column]);
        }

        private string TextValue(
            DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) ||
                row[column] == DBNull.Value)
                return "Unknown";

            return Convert.ToString(row[column]);
        }

        // ==================================================
        // REPORT 1: INCOME
        // Same source as WeeklyIncomeData.
        // ==================================================

        private void LoadIncome(
            DateTime start, DateTime end)
        {
            string sql = @"
                SELECT
                    CONVERT(date, paymentDate) AS ReportDate,
                    SUM(ISNULL(amountPaid, 0)) AS TotalIncome
                FROM dbo.Payment
                WHERE paymentDate >= @StartDate
                  AND paymentDate < DATEADD(DAY, 1, @EndDate)
                GROUP BY CONVERT(date, paymentDate)
                ORDER BY ReportDate";

            DataTable dt = Query(sql, start, end);

            List<string> labels = new List<string>();
            List<decimal> values = new List<decimal>();

            foreach (DataRow row in dt.Rows)
            {
                DateTime date =
                    Convert.ToDateTime(row["ReportDate"]);

                labels.Add(date.ToString("dd MMM yyyy"));
                values.Add(Money(row, "TotalIncome"));
            }

            hfIncomeData.Value = ChartJson(labels, values);
        }

        // ==================================================
        // REPORT 2: EXPENSES
        // Same fuel + dumping definition as desktop.
        // ==================================================

        private void LoadExpenses(
            DateTime start, DateTime end)
        {
            string sql = @"
                SELECT
                    ISNULL(SUM(totalFuelCost), 0) AS Fuel,
                    ISNULL(SUM(dumpingCost), 0) AS Dumping
                FROM dbo.Job
                WHERE startDate >= @StartDate
                  AND startDate < DATEADD(DAY, 1, @EndDate)";

            DataTable dt = Query(sql, start, end);

            decimal fuel = 0m;
            decimal dumping = 0m;

            if (dt.Rows.Count > 0)
            {
                fuel = Money(dt.Rows[0], "Fuel");
                dumping = Money(dt.Rows[0], "Dumping");
            }

            hfExpenseData.Value = ChartJson(
                new List<string> { "Fuel", "Dumping" },
                new List<decimal> { fuel, dumping });
        }

        // ==================================================
        // REPORT 3: PROFITS
        // Mirrors DBConnect.WeeklyProfitData.
        // ==================================================

        private void LoadProfits(
            DateTime start, DateTime end)
        {
            string sql = @"
                WITH Income AS
                (
                    SELECT
                        p.jobID,
                        SUM(p.amountPaid) AS Income
                    FROM dbo.Payment p
                    WHERE p.paymentDate >= @StartDate
                      AND p.paymentDate <
                          DATEADD(DAY, 1, @EndDate)
                    GROUP BY p.jobID
                )
                SELECT
                    j.jobID,
                    j.startDate AS JobDate,
                    ISNULL(i.Income, 0) AS Income,
                    ISNULL(j.totalFuelCost, 0) AS FuelCost,
                    ISNULL(j.dumpingCost, 0) AS DumpingCost
                FROM dbo.Job j
                LEFT JOIN Income i
                    ON j.jobID = i.jobID
                WHERE j.startDate >= @StartDate
                  AND j.startDate <
                      DATEADD(DAY, 1, @EndDate)
                ORDER BY j.startDate";

            DataTable dt = Query(sql, start, end);

            SortedDictionary<DateTime, decimal[]> totals =
                new SortedDictionary<DateTime, decimal[]>();

            foreach (DataRow row in dt.Rows)
            {
                if (row["JobDate"] == DBNull.Value)
                    continue;

                DateTime date =
                    Convert.ToDateTime(row["JobDate"]).Date;

                if (!totals.ContainsKey(date))
                    totals[date] = new decimal[2];

                totals[date][0] += Money(row, "Income");

                totals[date][1] +=
                    Money(row, "FuelCost") +
                    Money(row, "DumpingCost");
            }

            List<string> labels = new List<string>();
            List<decimal> incomes = new List<decimal>();
            List<decimal> expenses = new List<decimal>();
            List<decimal> profits = new List<decimal>();

            foreach (var item in totals)
            {
                decimal income = item.Value[0];
                decimal expense = item.Value[1];

                labels.Add(
                    item.Key.ToString("dd MMM yyyy"));

                incomes.Add(income);
                expenses.Add(expense);
                profits.Add(income - expense);
            }

            hfProfitData.Value = Json(new
            {
                labels = labels,
                income = incomes,
                expenses = expenses,
                profit = profits
            });
        }

        // ==================================================
        // REPORT 4: SERVICE PROFITABILITY
        // Mirrors DBConnect.JobTypeProfitabilityData.
        // ==================================================

        private void LoadServiceProfitability(
            DateTime start, DateTime end)
        {
            string sql = @"
                WITH AssetCosts AS
                (
                    SELECT
                        ja.jobID,
                        SUM(
                            CASE
                                WHEN ja.hiredAssetID IS NOT NULL
                                THEN ha.hireCost
                                ELSE 0
                            END
                        ) AS AssetCost
                    FROM dbo.JobAssetAssignment ja
                    LEFT JOIN dbo.HiredAsset ha
                        ON ja.hiredAssetID = ha.hiredAssetID
                    GROUP BY ja.jobID
                ),
                CompletedJobData AS
                (
                    SELECT
                        jt.jobTypeID,
                        jt.jobTypeName AS JobType,

                        COUNT(DISTINCT j.jobID)
                            AS NumberOfJobs,

                        SUM(
                            CAST(id.detailValue AS DECIMAL(10,2))
                            * jt.jobRate
                        ) AS TotalRevenue,

                        SUM(ISNULL(j.totalFuelCost, 0))
                            AS FuelCost,

                        SUM(ISNULL(j.dumpingCost, 0))
                            AS DumpingCost,

                        SUM(ISNULL(ac.AssetCost, 0))
                            AS AssetCost

                    FROM dbo.Job j

                    INNER JOIN dbo.Quote q
                        ON j.quoteID = q.QuoteID

                    INNER JOIN dbo.JobRequest jr
                        ON q.jobRequestID = jr.jobRequestID

                    INNER JOIN dbo.RequestItem ri
                        ON jr.jobRequestID = ri.jobRequestID

                    INNER JOIN dbo.JobType jt
                        ON ri.jobTypeID = jt.jobTypeID

                    INNER JOIN dbo.ItemDetail id
                        ON ri.requestItemID = id.requestItemID

                    LEFT JOIN AssetCosts ac
                        ON j.jobID = ac.jobID

                    WHERE j.startDate >= @StartDate
                      AND j.startDate <
                          DATEADD(DAY, 1, @EndDate)

                      AND j.jobStatus = 'Completed'

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

                    GROUP BY
                        jt.jobTypeID,
                        jt.jobTypeName
                )

                SELECT
                    jt.jobTypeName AS JobType,

                    ISNULL(cjd.NumberOfJobs, 0)
                        AS NumberOfJobs,

                    ISNULL(cjd.TotalRevenue, 0)
                        AS TotalRevenue,

                    ISNULL(cjd.FuelCost, 0)
                        AS FuelCost,

                    ISNULL(cjd.DumpingCost, 0)
                        AS DumpingCost,

                    ISNULL(cjd.AssetCost, 0)
                        AS AssetCost

                FROM dbo.JobType jt

                LEFT JOIN CompletedJobData cjd
                    ON jt.jobTypeID = cjd.jobTypeID

                ORDER BY jt.jobTypeName";

            DataTable dt = Query(sql, start, end);

            List<string> labels = new List<string>();
            List<decimal> values = new List<decimal>();

            foreach (DataRow row in dt.Rows)
            {
                decimal revenue =
                    Money(row, "TotalRevenue");

                decimal totalCosts =
                    Money(row, "FuelCost") +
                    Money(row, "DumpingCost") +
                    Money(row, "AssetCost");

                decimal profit = revenue - totalCosts;

                labels.Add(TextValue(row, "JobType"));
                values.Add(profit);
            }

            hfServiceData.Value =
                ChartJson(labels, values);
        }

        // ==================================================
        // REPORT 5: QUOTE CONVERSION
        // Mirrors DBConnect.QuoteConversionData.
        // ==================================================

        private void LoadQuoteConversion(
            DateTime start, DateTime end)
        {
            string sql = @"
                SELECT
                    jr.requestSource AS RequestSource,

                    COUNT(DISTINCT jr.jobRequestID)
                        AS TotalRequests,

                    COUNT(DISTINCT q.QuoteID)
                        AS QuotesGenerated,

                    COUNT(DISTINCT CASE
                        WHEN q.quoteStatus IN
                            ('Sent', 'Accepted', 'Rejected')
                        THEN q.QuoteID
                    END) AS QuotesSent,

                    COUNT(DISTINCT CASE
                        WHEN q.quoteStatus = 'Accepted'
                        THEN q.QuoteID
                    END) AS QuotesAccepted,

                    COUNT(DISTINCT CASE
                        WHEN q.quoteStatus = 'Rejected'
                        THEN q.QuoteID
                    END) AS QuotesRejected,

                    COUNT(DISTINCT CASE
                        WHEN jr.status = 'Cancelled'
                        THEN jr.jobRequestID
                    END) AS RequestsCancelled,

                    COUNT(DISTINCT j.jobID)
                        AS JobsCreated,

                    ISNULL(AVG(q.amount), 0)
                        AS AverageQuotedValue,

                    ISNULL(SUM(CASE
                        WHEN q.quoteStatus = 'Accepted'
                        THEN q.amount
                        ELSE 0
                    END), 0) AS AcceptedQuoteRevenue

                FROM dbo.JobRequest jr

                LEFT JOIN dbo.Quote q
                    ON jr.jobRequestID = q.jobRequestID

                LEFT JOIN dbo.Job j
                    ON q.QuoteID = j.quoteID

                WHERE jr.dateRecieved >= @StartDate
                  AND jr.dateRecieved <
                      DATEADD(DAY, 1, @EndDate)

                GROUP BY jr.requestSource

                ORDER BY jr.requestSource";

            DataTable dt = Query(sql, start, end);

            List<string> labels = new List<string>();
            List<decimal> values = new List<decimal>();

            foreach (DataRow row in dt.Rows)
            {
                decimal requests =
                    Money(row, "TotalRequests");

                decimal accepted =
                    Money(row, "QuotesAccepted");

                decimal rate = requests > 0
                    ? accepted / requests * 100m
                    : 0m;

                labels.Add(
                    TextValue(row, "RequestSource"));

                values.Add(
                    Math.Round(rate, 2));
            }

            hfQuoteData.Value =
                ChartJson(labels, values);
        }

        // ==================================================
        // REPORT 6: CLIENT VALUE
        // Mirrors DBConnect.ClientValueData.
        // ==================================================

        private void LoadClientValue(
            DateTime start,
            DateTime end,
            string clientType)
        {
            string sql = @"
                WITH PaymentTotals AS
                (
                    SELECT
                        jobID,
                        SUM(amountPaid) AS TotalPaid
                    FROM dbo.Payment
                    GROUP BY jobID
                )

                SELECT
                    c.clientID AS ClientID,

                    c.clientName + ' ' + c.clientSurname
                        AS Client,

                    c.clientType AS ClientType,

                    COUNT(DISTINCT jr.jobRequestID)
                        AS NumberOfRequests,

                    COUNT(DISTINCT CASE
                        WHEN j.jobStatus = 'Completed'
                        THEN j.jobID
                    END) AS CompletedJobs,

                    ISNULL(
                        SUM(DISTINCT q.amount), 0
                    ) AS TotalAmountQuoted,

                    ISNULL(
                        SUM(
                            CASE
                                WHEN j.jobID IS NOT NULL
                                THEN ISNULL(pt.TotalPaid, 0)
                                ELSE 0
                            END
                        ), 0
                    ) AS TotalAmountPaid,

                    ISNULL(
                        SUM(
                            CASE
                                WHEN j.jobStatus = 'Completed'
                                THEN q.amount
                                ELSE 0
                            END
                        ) /
                        NULLIF(
                            COUNT(DISTINCT CASE
                                WHEN j.jobStatus = 'Completed'
                                THEN j.jobID
                            END), 0
                        ), 0
                    ) AS AverageJobValue,

                    COUNT(DISTINCT CASE
                        WHEN jr.status = 'Cancelled'
                             OR q.quoteStatus = 'Rejected'
                        THEN jr.jobRequestID
                    END) AS CancelledRejectedRequests,

                    ISNULL(
                        MAX(
                            CASE
                                WHEN j.jobStatus = 'Completed'
                                THEN j.startDate
                            END
                        ), '1900-01-01'
                    ) AS LastJobDate

                FROM dbo.Client c

                LEFT JOIN dbo.JobRequest jr
                    ON c.clientID = jr.clientID
                    AND jr.dateRecieved >= @StartDate
                    AND jr.dateRecieved <
                        DATEADD(DAY, 1, @EndDate)

                LEFT JOIN dbo.Quote q
                    ON jr.jobRequestID = q.jobRequestID

                LEFT JOIN dbo.Job j
                    ON q.QuoteID = j.quoteID

                LEFT JOIN PaymentTotals pt
                    ON j.jobID = pt.jobID

                WHERE
                    (@ClientType = 'All'
                     OR c.clientType = @ClientType)

                GROUP BY
                    c.clientID,
                    c.clientName,
                    c.clientSurname,
                    c.clientType

                HAVING
                    COUNT(DISTINCT jr.jobRequestID) > 0

                ORDER BY
                    TotalAmountPaid DESC,
                    TotalAmountQuoted DESC";

            DataTable dt = Query(
                sql, start, end, clientType);

            List<string> labels = new List<string>();
            List<decimal> values = new List<decimal>();

            int count = 0;

            foreach (DataRow row in dt.Rows)
            {
                if (count >= 10)
                    break;

                labels.Add(
                    TextValue(row, "Client"));

                values.Add(
                    Money(row, "TotalAmountPaid"));

                count++;
            }

            hfClientData.Value =
                ChartJson(labels, values);
        }

        // Popular areas: request-date cohort, 1 row per request before aggregation.
        // Address grouping uses the last comma-separated component of siteAddress.
        // Payments are total recorded to date for jobs linked to these requests.
        // Estimated contribution uses completed jobs only (payments minus fuel/dumping).
        private void LoadPopularAreas(DateTime start, DateTime end)
        {
            string sql = @"
                WITH PaymentsPerJob AS
                (
                    SELECT p.jobID,
                           SUM(CONVERT(decimal(19,2), p.amountPaid)) AS Received
                    FROM dbo.Payment p
                    GROUP BY p.jobID
                ),
                JobsPerRequest AS
                (
                    SELECT q.jobRequestID,
                           SUM(ISNULL(pp.Received, 0)) AS Received,
                           SUM(CASE WHEN j.jobStatus = 'Completed'
                               THEN ISNULL(pp.Received,0)
                                    - ISNULL(j.totalFuelCost,0)
                                    - ISNULL(j.dumpingCost,0)
                               ELSE 0 END) AS CompletedContribution,
                           SUM(CASE WHEN j.jobStatus = 'Completed'
                               THEN 1 ELSE 0 END) AS CompletedJobs
                    FROM dbo.Job j
                    INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID
                    LEFT JOIN PaymentsPerJob pp ON pp.jobID = j.jobID
                    GROUP BY q.jobRequestID
                ),
                RequestPeriod AS
                (
                    SELECT jr.jobRequestID,
                           jr.dateRecieved,
                           CASE WHEN UPPER(LTRIM(RTRIM(jr.requestSource))) = 'WEBSITE'
                                THEN 'Website' ELSE 'Other Sources' END AS SourceChannel,
                           CASE WHEN CHARINDEX(',', jr.siteAddress) > 0
                                THEN COALESCE(NULLIF(LTRIM(RTRIM(
                                    RIGHT(jr.siteAddress,
                                          CHARINDEX(',', REVERSE(jr.siteAddress)) - 1)
                                )), ''), '(Area not specified)')
                                ELSE '(Area not specified)' END AS Area
                    FROM dbo.JobRequest jr
                    WHERE jr.dateRecieved >= @StartDate
                      AND jr.dateRecieved < DATEADD(day, 1, @EndDate)
                )

                SELECT TOP (10)
                    rp.Area,
                    COUNT(*) AS RequestCount,
                    SUM(ISNULL(jpr.Received, 0)) AS PaymentsReceived,
                    SUM(ISNULL(jpr.CompletedContribution, 0)) AS EstimatedProfit,
                    SUM(ISNULL(jpr.CompletedJobs, 0)) AS CompletedJobs
                FROM RequestPeriod rp
                LEFT JOIN JobsPerRequest jpr ON jpr.jobRequestID = rp.jobRequestID
                GROUP BY rp.Area
                ORDER BY COUNT(*) DESC, SUM(ISNULL(jpr.Received,0)) DESC, rp.Area;";

            DataTable dt = Query(sql, start, end);
            List<string> labels = new List<string>();
            List<int> requests = new List<int>();
            List<decimal> payments = new List<decimal>();
            List<decimal> profit = new List<decimal>();
            List<int> completed = new List<int>();

            foreach (DataRow row in dt.Rows)
            {
                labels.Add(TextValue(row, "Area"));
                requests.Add(Convert.ToInt32(row["RequestCount"]));
                payments.Add(Money(row, "PaymentsReceived"));
                profit.Add(Money(row, "EstimatedProfit"));
                completed.Add(Convert.ToInt32(row["CompletedJobs"]));
            }

            hfAreaData.Value = Json(new
            {
                labels = labels,
                requests = requests,
                payments = payments,
                profit = profit,
                completedJobs = completed
            });
        }


        // Website source comparison is based on requestSource, not tracking analytics.
        // Payment totals are collected-to-date for requests created in the selected range.
        private void LoadWebsiteImpact(DateTime start, DateTime end)
        {
            string sql = @"
                WITH PaymentsPerJob AS
                (
                    SELECT p.jobID,
                           SUM(CONVERT(decimal(19,2), p.amountPaid)) AS Received
                    FROM dbo.Payment p
                    GROUP BY p.jobID
                ),
                JobsPerRequest AS
                (
                    SELECT q.jobRequestID,
                           SUM(ISNULL(pp.Received, 0)) AS Received,
                           SUM(CASE WHEN j.jobStatus = 'Completed'
                               THEN ISNULL(pp.Received,0)
                                    - ISNULL(j.totalFuelCost,0)
                                    - ISNULL(j.dumpingCost,0)
                               ELSE 0 END) AS CompletedContribution,
                           SUM(CASE WHEN j.jobStatus = 'Completed'
                               THEN 1 ELSE 0 END) AS CompletedJobs
                    FROM dbo.Job j
                    INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID
                    LEFT JOIN PaymentsPerJob pp ON pp.jobID = j.jobID
                    GROUP BY q.jobRequestID
                ),
                RequestPeriod AS
                (
                    SELECT jr.jobRequestID,
                           jr.dateRecieved,
                           CASE WHEN UPPER(LTRIM(RTRIM(jr.requestSource))) = 'WEBSITE'
                                THEN 'Website' ELSE 'Other Sources' END AS SourceChannel,
                           CASE WHEN CHARINDEX(',', jr.siteAddress) > 0
                                THEN COALESCE(NULLIF(LTRIM(RTRIM(
                                    RIGHT(jr.siteAddress,
                                          CHARINDEX(',', REVERSE(jr.siteAddress)) - 1)
                                )), ''), '(Area not specified)')
                                ELSE '(Area not specified)' END AS Area
                    FROM dbo.JobRequest jr
                    WHERE jr.dateRecieved >= @StartDate
                      AND jr.dateRecieved < DATEADD(day, 1, @EndDate)
                )

                SELECT rp.SourceChannel,
                       COUNT(*) AS RequestCount,
                       SUM(ISNULL(jpr.Received,0)) AS PaymentsReceived,
                       SUM(ISNULL(jpr.CompletedContribution,0)) AS EstimatedProfit,
                       SUM(ISNULL(jpr.CompletedJobs,0)) AS CompletedJobs
                FROM RequestPeriod rp
                LEFT JOIN JobsPerRequest jpr ON jpr.jobRequestID = rp.jobRequestID
                GROUP BY rp.SourceChannel;";

            DataTable dt = Query(sql, start, end);
            int webRequests = 0, otherRequests = 0;
            decimal webPayments = 0, otherPayments = 0;
            decimal webProfit = 0, otherProfit = 0;
            int webCompleted = 0, otherCompleted = 0;

            foreach (DataRow row in dt.Rows)
            {
                bool isWebsite = TextValue(row, "SourceChannel") == "Website";
                int count = Convert.ToInt32(row["RequestCount"]);
                decimal received = Money(row, "PaymentsReceived");
                decimal contribution = Money(row, "EstimatedProfit");
                int completed = Convert.ToInt32(row["CompletedJobs"]);
                if (isWebsite)
                {
                    webRequests = count;
                    webPayments = received;
                    webProfit = contribution;
                    webCompleted = completed;
                }
                else
                {
                    otherRequests = count;
                    otherPayments = received;
                    otherProfit = contribution;
                    otherCompleted = completed;
                }
            }

            // Monthly breakdown of website-originated requests from the same cohort.
            string monthSql = @"
                WITH PaymentsPerJob AS
                (
                    SELECT p.jobID,
                           SUM(CONVERT(decimal(19,2), p.amountPaid)) AS Received
                    FROM dbo.Payment p
                    GROUP BY p.jobID
                ),
                JobsPerRequest AS
                (
                    SELECT q.jobRequestID,
                           SUM(ISNULL(pp.Received, 0)) AS Received,
                           SUM(CASE WHEN j.jobStatus = 'Completed'
                               THEN ISNULL(pp.Received,0)
                                    - ISNULL(j.totalFuelCost,0)
                                    - ISNULL(j.dumpingCost,0)
                               ELSE 0 END) AS CompletedContribution,
                           SUM(CASE WHEN j.jobStatus = 'Completed'
                               THEN 1 ELSE 0 END) AS CompletedJobs
                    FROM dbo.Job j
                    INNER JOIN dbo.Quote q ON q.QuoteID = j.quoteID
                    LEFT JOIN PaymentsPerJob pp ON pp.jobID = j.jobID
                    GROUP BY q.jobRequestID
                ),
                RequestPeriod AS
                (
                    SELECT jr.jobRequestID,
                           jr.dateRecieved,
                           CASE WHEN UPPER(LTRIM(RTRIM(jr.requestSource))) = 'WEBSITE'
                                THEN 'Website' ELSE 'Other Sources' END AS SourceChannel,
                           CASE WHEN CHARINDEX(',', jr.siteAddress) > 0
                                THEN COALESCE(NULLIF(LTRIM(RTRIM(
                                    RIGHT(jr.siteAddress,
                                          CHARINDEX(',', REVERSE(jr.siteAddress)) - 1)
                                )), ''), '(Area not specified)')
                                ELSE '(Area not specified)' END AS Area
                    FROM dbo.JobRequest jr
                    WHERE jr.dateRecieved >= @StartDate
                      AND jr.dateRecieved < DATEADD(day, 1, @EndDate)
                )

                SELECT YEAR(rp.dateRecieved) AS Y,
                       MONTH(rp.dateRecieved) AS M,
                       COUNT(*) AS RequestCount,
                       SUM(ISNULL(jpr.Received,0)) AS PaymentsReceived
                FROM RequestPeriod rp
                LEFT JOIN JobsPerRequest jpr ON jpr.jobRequestID = rp.jobRequestID
                WHERE rp.SourceChannel = 'Website'
                GROUP BY YEAR(rp.dateRecieved), MONTH(rp.dateRecieved)
                ORDER BY Y, M;";

            DataTable months = Query(monthSql, start, end);
            List<string> monthLabels = new List<string>();
            List<int> monthRequests = new List<int>();
            List<decimal> monthPayments = new List<decimal>();
            foreach (DataRow row in months.Rows)
            {
                monthLabels.Add(new DateTime(
                    Convert.ToInt32(row["Y"]),
                    Convert.ToInt32(row["M"]), 1).ToString("MMM yyyy"));
                monthRequests.Add(Convert.ToInt32(row["RequestCount"]));
                monthPayments.Add(Money(row, "PaymentsReceived"));
            }

            int totalRequests = webRequests + otherRequests;
            decimal totalPayments = webPayments + otherPayments;

            hfWebsiteData.Value = Json(new
            {
                labels = new[] { "Website", "Other Sources" },
                requests = new[] { webRequests, otherRequests },
                payments = new[] { webPayments, otherPayments },
                profit = new[] { webProfit, otherProfit },
                completedJobs = new[] { webCompleted, otherCompleted },
                websiteRequestShare = totalRequests == 0 ? 0 :
                    Math.Round((decimal)webRequests / totalRequests * 100m, 1),
                websitePaymentShare = totalPayments == 0 ? 0 :
                    Math.Round(webPayments / totalPayments * 100m, 1),
                websiteMonthlyLabels = monthLabels,
                websiteMonthlyRequests = monthRequests,
                websiteMonthlyPayments = monthPayments
            });
        }

    }
}
