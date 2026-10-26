using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class RequestQuote : Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["defaultConnection"].ConnectionString;

        // Hardcoded Business Location Constants
        private const double BaseLatitude = -29.890840081918007;
        private const double BaseLongitude = 30.905937134956915;
        private const decimal CostPerKilometer = 36.00m;
        private const decimal BaseCallOutFee = 500.00m;
        private const decimal FlatTravelFee = 850.00m;   // used when the job has no coordinates
        private const decimal VatRate = 0.15m;

        private enum AccessRole { None, Management, Client }
        private AccessRole _role = AccessRole.None;
        private int _clientId;

        /// <summary>
        /// The chosen job request is held in ViewState (tamper-proof) - not read back from a textbox or hidden field,
        /// both of which the browser can edit before posting.
        /// </summary>
        private int SelectedJobId
        {
            get { return ViewState["SelectedJobId"] is int id ? id : 0; }
            set { ViewState["SelectedJobId"] = value; }
        }

        private sealed class JobInfo
        {
            public string Address;
            public double Latitude;
            public double Longitude;
            public string Status;
        }

        private sealed class QuoteFigures
        {
            public decimal Services, Travel, Subtotal, Vat, Total;
            public double OneWayKm;
            public string TravelBreakdown;
        }

        // ------------------------------------------------------------------
        //  Page lifecycle + access control
        // ------------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // Checked on EVERY request, so a postback after the session expires can't slip through.
            if (!ResolveAccess())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindJobRequests();
            }
        }
        private void BindJobRequests()
        {
            string query = @"SELECT jr.jobRequestID, jr.siteAddress, jr.urgencyLevel, jr.dateRecieved, jr.status, c.clientName
                             FROM JobRequest jr
                             LEFT JOIN Client c ON c.clientID = jr.clientID
                             WHERE jr.status NOT IN ('Cancelled', 'Quote Sent', 'Quote Accepted') ";

            if (!IsManagementView)
                query += "AND jr.clientID = @ClientID ";

            query += "ORDER BY jr.dateRecieved DESC";

            // Management sees which client each request belongs to (column index 1 = "Client").
            gvJobRequests.Columns[1].Visible = IsManagementView;

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!IsManagementView)
                    cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = _clientId;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvJobRequests.DataSource = dt;
                    gvJobRequests.DataBind();

                    bool hasData = dt.Rows.Count > 0;

                    pnlJobSelection.Visible = hasData;
                    pnlEmptyState.Visible = !hasData;

                    // On fresh query, if no row is selected, hide details and show prompt
                    if (gvJobRequests.SelectedIndex < 0)
                    {
                        pnlFormContent.Visible = false;
                        pnlSelectionDetails.Visible = false;
                        pnlSelectionPrompt.Visible = true;
                    }
                }
            }
        }

        private bool ResolveAccess()
        {
            _role = AccessRole.None;
            _clientId = 0;

            // 1. Owner (6) or Admin (5)
            if (Session["UserLevel"] != null
                && int.TryParse(Session["UserLevel"].ToString(), out int level)
                && (level == 5 || level == 6))
            {
                _role = AccessRole.Management;
                return true;
            }

            // 2. Standard client
            if (Session["ClientID"] != null
                && int.TryParse(Session["ClientID"].ToString(), out int clientId)
                && clientId > 0)
            {
                _role = AccessRole.Client;
                _clientId = clientId;
                return true;
            }

            return false;
        }

        private bool IsManagementView { get { return _role == AccessRole.Management; } }

        // ------------------------------------------------------------------
        //  Display helpers used by the .aspx
        // ------------------------------------------------------------------
        protected string GetUrgencyCss(object urgency)
        {
            string u = Convert.ToString(urgency)?.Trim().ToLowerInvariant() ?? string.Empty;
            if (u.Contains("urgent") || u.Contains("high") || u.Contains("emergency") || u.Contains("critical"))
                return "badge rounded-pill bg-danger";
            if (u.Contains("medium") || u.Contains("normal") || u.Contains("standard"))
                return "badge rounded-pill bg-warning text-dark";
            if (u.Contains("low"))
                return "badge rounded-pill bg-success";
            return "badge rounded-pill bg-secondary";
        }

        // ------------------------------------------------------------------
        //  Job request list
        // ------------------------------------------------------------------

        private void BindRequestItems(int jobRequestId)
        {
            string query = @"
        SELECT
            jt.JobTypeName,
            jt.JobRate,
            COALESCE(billable.Quantity, 0) AS Quantity,
            CASE
                WHEN jt.JobTypeName LIKE '%Grass%'
                  OR jt.JobTypeName LIKE '%Paving%'
                  OR jt.JobTypeName LIKE '%Tiling%'
                    THEN 'm²'
                WHEN jt.JobTypeName LIKE '%Tree%'
                  OR jt.JobTypeName LIKE '%Stump%'
                    THEN 'Trees / Units'
                WHEN jt.JobTypeName LIKE '%Refuse%'
                  OR jt.JobTypeName LIKE '%Cartage%'
                    THEN 'm³'
                ELSE 'Item(s)'
            END AS UnitOfMeasure,
            CAST(
                jt.JobRate * COALESCE(billable.Quantity, 0)
                AS DECIMAL(18, 2)
            ) AS LineTotal
        FROM RequestItem ri
        INNER JOIN JobType jt
            ON ri.jobTypeID = jt.jobTypeID
        OUTER APPLY
        (
            SELECT TOP (1)
                TRY_CONVERT(DECIMAL(18, 2), id.detailValue) AS Quantity
            FROM ItemDetail id
            WHERE id.requestItemID = ri.requestItemID
              AND id.jobDetailID =
                  CASE
                      WHEN jt.JobTypeName = 'Tree Felling' THEN 1
                      WHEN jt.JobTypeName = 'Grass Cutting' THEN 6
                      WHEN jt.JobTypeName = 'Tree Planting' THEN 10
                      WHEN jt.JobTypeName = 'Vegetation Clearance' THEN 14
                      WHEN jt.JobTypeName = 'Hedge Trimming' THEN 18
                      ELSE NULL
                  END
            ORDER BY id.jobDetailID
        ) billable
        WHERE ri.jobRequestID = @JobRequestID
        ORDER BY jt.JobTypeName;";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@JobRequestID", SqlDbType.Int).Value =
                    jobRequestId;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvRequestItems.DataSource = dt;
                    gvRequestItems.DataBind();
                }
            }
        }


        protected void gvJobRequests_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_role == AccessRole.None) return;

            int jobRequestId = Convert.ToInt32(gvJobRequests.SelectedDataKey.Value);

            // Ownership check: re-load the job from the DB restricted to what this user may see.
            if (!TryLoadJob(jobRequestId, out JobInfo job))
            {
                ResetForm();
                ShowMessage("That job request could not be found.", "alert alert-warning rounded-3 mb-4", false);
                return;
            }

            SelectedJobId = jobRequestId;
            txtSelectedJobId.Text = jobRequestId.ToString();
            txtSiteAddress.Text = job.Address;

            // 1. Show the selection details & hide the initial prompt
            pnlSelectionDetails.Visible = true;
            pnlSelectionPrompt.Visible = false;

            // 2. Show the lower quote sections (2, 3, and 4)
            pnlFormContent.Visible = true;
            pnlMessage.Visible = false;
            btnGenerateArbitraryQuote.Visible = true;
            btnViewPdfQuote.Visible = false;

            SetSteps(2);

            // 3. Load details and calculate totals (all server-side from the database)
            BindRequestItems(jobRequestId);
            QuoteFigures figures = CalculateQuoteFigures(jobRequestId, job);
            ApplyFigures(figures, job.Address);

            // 4. If a quote already exists, say so up-front instead of waiting for a failed save.
            if (QuoteExists(jobRequestId))
            {
                ShowMessage("A quote for this job request already exists. You can view it below.", "alert alert-info rounded-3 mb-4", true);
            }
        }

        // ------------------------------------------------------------------
        //  Data access
        // ------------------------------------------------------------------
        /// <summary>
        /// Loads a job request only if the signed-in user is allowed to see it (management: any; client: own only).
        /// This is the single gatekeeper for every action on the page.
        /// </summary>
        private bool TryLoadJob(int jobRequestId, out JobInfo job)
        {
            job = null;

            const string query = @"SELECT siteAddress, longitude, latitude, status
                                   FROM JobRequest
                                   WHERE jobRequestID = @JobRequestID
                                     AND (@IsStaff = 1 OR clientID = @ClientID)";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@JobRequestID", SqlDbType.Int).Value = jobRequestId;
                cmd.Parameters.Add("@IsStaff", SqlDbType.Bit).Value = IsManagementView;
                cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = _clientId;
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return false;

                    job = new JobInfo
                    {
                        Address = reader["siteAddress"] != DBNull.Value ? reader["siteAddress"].ToString() : string.Empty,
                        Longitude = reader["longitude"] != DBNull.Value ? Convert.ToDouble(reader["longitude"]) : 0.0,
                        Latitude = reader["latitude"] != DBNull.Value ? Convert.ToDouble(reader["latitude"]) : 0.0,
                        Status = reader["status"] != DBNull.Value ? reader["status"].ToString() : string.Empty
                    };
                    return true;
                }
            }
        }

        


        private decimal GetServicesSubtotal(int jobRequestId)
        {
            const string calcQuery = @"
        SELECT
            ISNULL(
                SUM(
                    jt.JobRate *
                    COALESCE(billable.Quantity, 0)
                ),
                0
            )
        FROM RequestItem ri
        INNER JOIN JobType jt
            ON ri.jobTypeID = jt.jobTypeID
        OUTER APPLY
        (
            SELECT TOP (1)
                TRY_CONVERT(DECIMAL(18, 2), id.detailValue) AS Quantity
            FROM ItemDetail id
            WHERE id.requestItemID = ri.requestItemID
              AND id.jobDetailID =
                 CASE
    WHEN jt.JobTypeName = 'Tree Felling' THEN 1
    WHEN jt.JobTypeName = 'Grass Cutting' THEN 6
    WHEN jt.JobTypeName = 'Tree Planting' THEN 10
    WHEN jt.JobTypeName = 'Vegetation Clearance' THEN 14
    WHEN jt.JobTypeName = 'Hedge Trimming' THEN 18
    ELSE NULL
END
        ) billable
        WHERE ri.jobRequestID = @JobRequestID;";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(calcQuery, conn))
            {
                cmd.Parameters.Add("@JobRequestID", SqlDbType.Int).Value =
                    jobRequestId;

                conn.Open();

                object result = cmd.ExecuteScalar();

                return result != null && result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0m;
            }
        }


        private bool QuoteExists(int jobRequestId)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Quote WHERE jobRequestID = @JobRequestID", conn))
            {
                cmd.Parameters.Add("@JobRequestID", SqlDbType.Int).Value = jobRequestId;
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        // ------------------------------------------------------------------
        //  Calculations (always from DB values - never from posted form fields)
        // ------------------------------------------------------------------
        private QuoteFigures CalculateQuoteFigures(int jobRequestId, JobInfo job)
        {
            QuoteFigures f = new QuoteFigures();
            f.Services = Math.Round(GetServicesSubtotal(jobRequestId), 2, MidpointRounding.AwayFromZero);

            if (job.Latitude == 0.0 || job.Longitude == 0.0)
            {
                f.OneWayKm = 0.0;
                f.TravelBreakdown = "Standard flat rate travel fee applied (Coordinates not specified).";
                f.Travel = FlatTravelFee;
            }
            else
            {
                // Haversine formula using BaseLatitude & BaseLongitude
                double dLat = (job.Latitude - BaseLatitude) * (Math.PI / 180.0);
                double dLng = (job.Longitude - BaseLongitude) * (Math.PI / 180.0);

                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(BaseLatitude * (Math.PI / 180.0)) * Math.Cos(job.Latitude * (Math.PI / 180.0)) *
                           Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                double oneWayDistance = 6371.0 * c; // Earth's radius ~6371 km

                decimal roundTripKm = Convert.ToDecimal(oneWayDistance) * 2;
                decimal distanceCost = roundTripKm * CostPerKilometer;

                f.OneWayKm = oneWayDistance;
                f.TravelBreakdown = $"Base Call-out: {BaseCallOutFee:C} + Round-trip ({roundTripKm:N1} km @ {CostPerKilometer:C}/km = {distanceCost:C})";
                f.Travel = Math.Round(BaseCallOutFee + distanceCost, 2, MidpointRounding.AwayFromZero);
            }

            f.Subtotal = f.Services + f.Travel;
            f.Vat = Math.Round(f.Subtotal * VatRate, 2, MidpointRounding.AwayFromZero);
            f.Total = f.Subtotal + f.Vat;
            return f;
        }

        private void ApplyFigures(QuoteFigures f, string address)
        {
            lblDisplaySiteAddress.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(address) ? "No address specified" : address);
            lblOneWayDistance.Text = $"{f.OneWayKm:N1} km";
            lblTravelBreakdown.Text = Server.HtmlEncode(f.TravelBreakdown);

            lblSubtotal.Text = f.Services.ToString("C");
            lblTravelFee.Text = f.Travel.ToString("C");
            lblTravelFeeSummary.Text = f.Travel.ToString("C");
            lblSubtotalWithTravel.Text = f.Subtotal.ToString("C");
            lblVat.Text = f.Vat.ToString("C");
            lblTotalWithVat.Text = f.Total.ToString("C");
        }

        // ------------------------------------------------------------------
        //  Save & generate
        // ------------------------------------------------------------------
        protected void btnGenerateArbitraryQuote_Click(object sender, EventArgs e)
        {
            if (_role == AccessRole.None) return;

            int jobRequestId = SelectedJobId;
            if (jobRequestId == 0)
            {
                ShowMessage("Please select an existing Job Request first.", "alert alert-warning rounded-3 mb-4", false);
                return;
            }

            // The job must belong to this user (clients) and still be eligible for a quote.
            if (!TryLoadJob(jobRequestId, out JobInfo job))
            {
                ResetForm();
                ShowMessage("That job request could not be found.", "alert alert-warning rounded-3 mb-4", false);
                return;
            }

            string status = job.Status ?? string.Empty;
            if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Quote Sent", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Quote Accepted", StringComparison.OrdinalIgnoreCase))
            {
                bool alreadyQuoted = !status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
                ShowMessage(alreadyQuoted
                    ? "A quote for this job request already exists. A new quote cannot be created."
                    : "This job request has been cancelled, so a quote cannot be created.",
                    "alert alert-warning rounded-3 mb-4", alreadyQuoted);
                return;
            }

            try
            {
                // 1. Item details / measurements must exist
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();


                    const string checkDetailsQuery = @"
    SELECT COUNT(*)
    FROM RequestItem ri
    INNER JOIN JobType jt
        ON ri.jobTypeID = jt.jobTypeID
    WHERE ri.jobRequestID = @JobRequestID
      AND EXISTS
      (
          SELECT 1
          FROM ItemDetail id
          WHERE id.requestItemID = ri.requestItemID
            AND id.jobDetailID =
                CASE
                    WHEN jt.JobTypeName = 'Tree Felling' THEN 1
                    WHEN jt.JobTypeName = 'Grass Cutting' THEN 6
                    WHEN jt.JobTypeName = 'Tree Planting' THEN 10
                    WHEN jt.JobTypeName = 'Vegetation Clearance' THEN 14
                    WHEN jt.JobTypeName = 'Hedge Trimming' THEN 18
                    ELSE NULL
                END
            AND TRY_CONVERT(DECIMAL(18, 2), id.detailValue) > 0
      );";

                    using (SqlCommand cmdDetails =
                           new SqlCommand(checkDetailsQuery, conn))
                    {
                        cmdDetails.Parameters.Add("@JobRequestID", SqlDbType.Int).Value =
                            jobRequestId;

                        int validItems = Convert.ToInt32(cmdDetails.ExecuteScalar());

                        if (validItems == 0)
                        {
                            ShowMessage(
                                "Cannot generate a quote yet. Valid billable quantities " +
                                "must be provided for the requested services.",
                                "alert alert-danger rounded-3 mb-4",
                                false);

                            return;
                        }
                    }

                }

                // 2. Recalculate everything on the server from database values
                QuoteFigures figures = CalculateQuoteFigures(jobRequestId, job);

                // 3. Insert quote + update job in ONE transaction. The NOT EXISTS makes the duplicate check atomic
                //    (previously two clicks at once could both pass the check and create two quotes).
                bool inserted;
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            const string insertQuoteQuery = @"
                                INSERT INTO Quote (jobRequestID, dateIssued, expiryDate, dateGenerated, amount, quoteStatus)
                                SELECT @JobRequestID, GETDATE(), DATEADD(month, 1, GETDATE()), GETDATE(), @Amount, 'Sent'
                                WHERE NOT EXISTS (SELECT 1 FROM Quote WHERE jobRequestID = @JobRequestID)";

                            using (SqlCommand cmdInsert = new SqlCommand(insertQuoteQuery, conn, tx))
                            {
                                cmdInsert.Parameters.Add("@JobRequestID", SqlDbType.Int).Value = jobRequestId;
                                SqlParameter amount = cmdInsert.Parameters.Add("@Amount", SqlDbType.Decimal);
                                amount.Precision = 18;
                                amount.Scale = 2;
                                amount.Value = figures.Total;
                                inserted = cmdInsert.ExecuteNonQuery() == 1;
                            }

                            if (inserted)
                            {
                                using (SqlCommand cmdJobUpdate = new SqlCommand("UPDATE JobRequest SET status = 'Quote Sent' WHERE jobRequestID = @JobRequestID", conn, tx))
                                {
                                    cmdJobUpdate.Parameters.Add("@JobRequestID", SqlDbType.Int).Value = jobRequestId;
                                    cmdJobUpdate.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }
                }

                if (!inserted)
                {
                    ShowMessage("A quote for this job request already exists. A new quote cannot be created.", "alert alert-warning rounded-3 mb-4", true);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RequestQuote generate error: " + ex);
                ShowMessage("We couldn't save the quote right now. Please try again shortly.", "alert alert-danger rounded-3 mb-4", false);
                return;
            }

            SetSteps(3);
            ShowMessage("Quote generated, saved and marked as Sent. Use the button on the right to open it.", "alert alert-success rounded-3 mb-4", true);
        }

        /// <summary>Highlights the progress bar: 1 = choosing, 2 = reviewing, 3 = generated.</summary>
        private void SetSteps(int step)
        {
            lblStepReview.CssClass = step >= 2 ? "step is-on" : "step";
            lblStepGenerate.CssClass = step >= 3 ? "step is-on" : "step";
        }

        private void ShowMessage(string message, string cssClass, bool showPdfButton)
        {
            lblMessage.Text = Server.HtmlEncode(message);
            pnlMessage.CssClass = cssClass;
            pnlMessage.Visible = true;
            btnViewPdfQuote.Visible = showPdfButton;

            // Once a quote exists there is nothing left to generate.
            btnGenerateArbitraryQuote.Visible = !showPdfButton;
        }

        // ------------------------------------------------------------------
        //  Cancel / view
        // ------------------------------------------------------------------
        protected void btnCancel_Click(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            gvJobRequests.SelectedIndex = -1;
            SelectedJobId = 0;
            txtSelectedJobId.Text = string.Empty;
            txtSiteAddress.Text = string.Empty;

            gvRequestItems.DataSource = null;
            gvRequestItems.DataBind();

            lblSubtotal.Text = "R 0,00";
            lblTravelFee.Text = "R 0,00";
            lblTravelFeeSummary.Text = "R 0,00";
            lblSubtotalWithTravel.Text = "R 0,00";
            lblVat.Text = "R 0,00";
            lblTotalWithVat.Text = "R 0,00";
            lblOneWayDistance.Text = "0.0 km";
            lblTravelBreakdown.Text = "Base Call-out + Distance calculation";
            lblDisplaySiteAddress.Text = "[No Address Selected]";

            btnViewPdfQuote.Visible = false;
            btnGenerateArbitraryQuote.Visible = true;
            pnlMessage.Visible = false;

            SetSteps(1);

            // Reset visibility states back to initial landing state
            pnlFormContent.Visible = false;
            pnlSelectionDetails.Visible = false;
            pnlSelectionPrompt.Visible = true;
        }

        protected void btnViewPdfQuote_Click(object sender, EventArgs e)
        {
            // PrintQuote.aspx performs its own ownership check, so only the owner/management can open it.
            int jobRequestId = SelectedJobId;
            if (_role != AccessRole.None && jobRequestId > 0)
            {
                Response.Redirect("~/PrintQuote.aspx?jobRequestID=" + jobRequestId);
            }
        }
    }
}