using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Web.UI;

namespace GiantGroupM3
{

    public partial class PrintQuote : Page
    {
        protected global::System.Web.UI.WebControls.Panel pnlQuote;
        protected global::System.Web.UI.WebControls.Panel pnlNotFound;
        protected global::System.Web.UI.WebControls.Label lblValidUntil;
        protected global::System.Web.UI.WebControls.Label lblStatus;
        protected global::System.Web.UI.WebControls.Label lblNotFound;
        protected global::System.Web.UI.WebControls.Repeater rptLines;
        protected global::System.Web.UI.HtmlControls.HtmlTableRow rowServicesSubtotal;
        // (and similarly for your repeaters/rows if needed)
        private const decimal VatRate = 0.15m;
        private static readonly CultureInfo ZA = CultureInfo.GetCultureInfo("en-ZA");

        private const string NotFoundMessage =
            "We couldn't find this quotation, or you don't have access to it. Go back to your quotes and try again.";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Quotes contain personal + financial data: never cache them in the browser or proxies.
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (IsPostBack) return;

            // ---- Authentication / authorisation (previously there was none) ----
            bool isStaff = IsManagement();
            int clientId = 0;
            bool isClient = !isStaff && TryGetClientId(out clientId);

            if (!isStaff && !isClient)
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            string raw = Request.QueryString["jobRequestID"];

            // No id supplied: keep the old session-preview behaviour for logged-in users.
            if (string.IsNullOrWhiteSpace(raw))
            {
                LoadQuoteFromSession();
                return;
            }

            // Strict integer parse - rejects anything that isn't a plain positive number.
            if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out int jobId) || jobId <= 0)
            {
                ShowNotFound(NotFoundMessage);
                return;
            }

            LoadQuoteFromDatabase(jobId, isStaff, clientId);
        }

        // ------------------------------------------------------------------
        //  Access helpers (same session keys used by RequestQuote / ViewQuote)
        // ------------------------------------------------------------------
        private bool IsManagement()
        {
            // Owner (6) or Admin (5)
            return Session["UserLevel"] != null
                && int.TryParse(Session["UserLevel"].ToString(), out int level)
                && (level == 5 || level == 6);
        }

        private bool TryGetClientId(out int clientId)
        {
            clientId = 0;
            return Session["ClientID"] != null
                && int.TryParse(Session["ClientID"].ToString(), out clientId)
                && clientId > 0;
        }

        private static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["GiantGroupDB"]?.ConnectionString
                ?? ConfigurationManager.ConnectionStrings["defaultConnection"]?.ConnectionString;
        }

        // ------------------------------------------------------------------
        //  Database load
        // ------------------------------------------------------------------
        private void LoadQuoteFromDatabase(int jobId, bool isStaff, int clientId)
        {
            string connString = GetConnectionString();
            if (string.IsNullOrEmpty(connString))
            {
                LoadQuoteFromSession();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();

                    int quoteId;
                    decimal total;
                    DateTime quoteDate;
                    DateTime? expiryDate = null;
                    string status, clientName, address;

                    // The WHERE clause is the access check: staff see any quote, clients only their own
                    // (and only once it has actually been sent to them).
                    const string quoteSql = @"
                        SELECT TOP 1 jr.siteAddress, jr.dateRecieved,
                               c.clientName,
                               q.QuoteID, q.dateIssued, q.expiryDate, q.amount, q.quoteStatus
                        FROM JobRequest jr
                        INNER JOIN Quote q ON q.jobRequestID = jr.jobRequestID
                        LEFT JOIN Client c ON c.clientID = jr.clientID
                        WHERE jr.jobRequestID = @JobId
                          AND (@IsStaff = 1
                               OR (jr.clientID = @ClientID AND q.quoteStatus IN ('Sent', 'Accepted', 'Rejected')))
                        ORDER BY q.dateIssued DESC, q.QuoteID DESC";

                    using (SqlCommand cmd = new SqlCommand(quoteSql, conn))
                    {
                        cmd.Parameters.Add("@JobId", SqlDbType.Int).Value = jobId;
                        cmd.Parameters.Add("@IsStaff", SqlDbType.Bit).Value = isStaff;
                        cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = clientId;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                ShowNotFound(NotFoundMessage);
                                return;
                            }

                            quoteId = Convert.ToInt32(reader["QuoteID"]);
                            total = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0m;
                            status = Convert.ToString(reader["quoteStatus"]) ?? string.Empty;
                            clientName = reader["clientName"] != DBNull.Value ? Convert.ToString(reader["clientName"]) : "Valued Client";
                            address = reader["siteAddress"] != DBNull.Value ? Convert.ToString(reader["siteAddress"]) : "Durban, KwaZulu-Natal";

                            if (reader["dateIssued"] != DBNull.Value)
                                quoteDate = Convert.ToDateTime(reader["dateIssued"]);
                            else if (reader["dateRecieved"] != DBNull.Value)
                                quoteDate = Convert.ToDateTime(reader["dateRecieved"]);
                            else
                                quoteDate = DateTime.Now;

                            if (reader["expiryDate"] != DBNull.Value)
                                expiryDate = Convert.ToDateTime(reader["expiryDate"]);
                        }
                    }

                    DataTable items = LoadLineItems(conn, jobId);

                    // ---- Figures ----
                    // The stored amount is the VAT-inclusive total (and may have been edited by management),
                    // so it is the source of truth. Travel is whatever remains after the line items.
                    decimal subtotal = Math.Round(total / (1m + VatRate), 2, MidpointRounding.AwayFromZero);
                    decimal vat = total - subtotal;

                    decimal itemsSum = 0m;
                    foreach (DataRow row in items.Rows)
                        itemsSum += Convert.ToDecimal(row["LineTotal"]);
                    itemsSum = Math.Round(itemsSum, 2);

                    decimal travel = subtotal - itemsSum;
                    bool reconciles = items.Rows.Count > 0 && travel >= 0m;

                    DataTable lines = NewLinesTable();
                    decimal servicesSubtotal;
                    decimal travelFee;

                    if (reconciles)
                    {
                        foreach (DataRow row in items.Rows)
                        {
                            decimal rate = Convert.ToDecimal(row["JobRate"]);
                            decimal qty = Convert.ToDecimal(row["Quantity"]);
                            lines.Rows.Add(
                                Convert.ToString(row["JobTypeName"]),
                                "Rate: " + FormatMoney(rate),
                                qty.ToString("0.##", ZA) + " " + Convert.ToString(row["UnitOfMeasure"]),
                                FormatMoney(Convert.ToDecimal(row["LineTotal"])));
                        }
                        servicesSubtotal = itemsSum;
                        travelFee = travel;
                    }
                    else
                    {
                        // Line items missing or no longer match the stored amount: show a single summary line.
                        lines.Rows.Add(
                            $"Professional Tree Felling & Site Clearance (Job #{jobId})",
                            string.Empty, string.Empty, FormatMoney(subtotal));
                        servicesSubtotal = subtotal;
                        travelFee = 0m;
                    }

                    rptLines.DataSource = lines;
                    rptLines.DataBind();
                    rowServicesSubtotal.Visible = reconciles;

                    // ---- Labels (everything from the DB is HTML-encoded: Label.Text does not encode) ----
                    lblQuoteDate.Text = quoteDate.ToString("dd MMMM yyyy", ZA);
                    lblQuoteNo.Text = $"Quotation NO. GT – {jobId}";

                    lblRecipientName.Text = Server.HtmlEncode(string.IsNullOrWhiteSpace(clientName) ? "Valued Client" : clientName);
                    lblRecipientAddress.Text = EncodeMultiline(address);

                    lblServicesSubtotal.Text = FormatMoney(servicesSubtotal);
                    lblTravelFee.Text = FormatMoney(travelFee);
                    lblSubtotalInclTravel.Text = FormatMoney(subtotal);
                    lblVat.Text = FormatMoney(vat);
                    lblTotalAmount.Text = FormatMoney(total);

                    ApplyValidity(expiryDate, status);
                    ApplyStatusBadge(status);

                    // Keep recording the document path on the Quote row (only writes when it changed).
                    RecordFilePath(conn, quoteId, jobId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PrintQuote error: " + ex);
                ShowNotFound("We couldn't load this quotation right now. Please try again shortly.");
            }
        }

        private static DataTable LoadLineItems(SqlConnection conn, int jobId)
        {
            const string sql = @"
                SELECT jt.JobTypeName, jt.JobRate,
                       ISNULL(id.detailValue, 1.00) AS Quantity,
                       CASE
                           WHEN jt.JobTypeName LIKE '%Grass%' OR jt.JobTypeName LIKE '%Paving%' OR jt.JobTypeName LIKE '%Tiling%' THEN 'm²'
                           WHEN jt.JobTypeName LIKE '%Tree%' OR jt.JobTypeName LIKE '%Stump%' THEN 'Trees / Units'
                           WHEN jt.JobTypeName LIKE '%Refuse%' OR jt.JobTypeName LIKE '%Cartage%' THEN 'm³'
                           ELSE 'Item(s)'
                       END AS UnitOfMeasure,
                       (jt.JobRate * ISNULL(id.detailValue, 1.00)) AS LineTotal
                FROM RequestItem ri
                INNER JOIN JobType jt ON ri.jobTypeID = jt.jobTypeID
                LEFT JOIN ItemDetail id ON ri.requestItemID = id.requestItemID
                WHERE ri.jobRequestID = @JobRequestID";

            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@JobRequestID", SqlDbType.Int).Value = jobId;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        private static void RecordFilePath(SqlConnection conn, int quoteId, int jobId)
        {
            try
            {
                const string sql = @"UPDATE Quote SET filePath = @FilePath
                                     WHERE QuoteID = @QuoteID
                                       AND (filePath IS NULL OR filePath <> @FilePath)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 260).Value = $"~/PrintQuote.aspx?jobRequestID={jobId}";
                    cmd.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Never stop a quote from displaying because of bookkeeping.
                Debug.WriteLine("PrintQuote filePath update failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        //  Session preview (legacy behaviour, now without hard-coded demo data)
        // ------------------------------------------------------------------
        private void LoadQuoteFromSession()
        {
            if (Session["Quote_Total"] == null)
            {
                ShowNotFound("There is no quotation to display. Select a quote from your list to view it.");
                return;
            }

            lblQuoteDate.Text = DateTime.Now.ToString("dd MMMM yyyy", ZA);
            lblQuoteNo.Text = "Quotation NO. GT – Draft";
            lblRecipientName.Text = "Valued Client";
            lblRecipientAddress.Text = string.Empty;

            DataTable lines = NewLinesTable();
            lines.Rows.Add("Services Rendered", string.Empty, string.Empty, SessionText("Quote_ServicesSubtotal"));
            rptLines.DataSource = lines;
            rptLines.DataBind();
            rowServicesSubtotal.Visible = false;

            lblServicesSubtotal.Text = SessionText("Quote_ServicesSubtotal");
            lblTravelFee.Text = SessionText("Quote_TravelFee");
            lblSubtotalInclTravel.Text = SessionText("Quote_SubtotalInclTravel");
            lblVat.Text = SessionText("Quote_Vat");
            lblTotalAmount.Text = SessionText("Quote_Total");
        }

        private string SessionText(string key)
        {
            string value = Session[key]?.ToString();
            return Server.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "R 0,00" : value);
        }

        // ------------------------------------------------------------------
        //  Display helpers
        // ------------------------------------------------------------------
        private static DataTable NewLinesTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Description", typeof(string));
            dt.Columns.Add("Detail", typeof(string));
            dt.Columns.Add("Qty", typeof(string));
            dt.Columns.Add("Amount", typeof(string));
            return dt;
        }

        private static string FormatMoney(decimal value)
        {
            return "R " + value.ToString("N2", ZA);
        }

        private string EncodeMultiline(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Server.HtmlEncode(text.Trim())
                         .Replace("\r\n", "<br />")
                         .Replace("\n", "<br />");
        }

        private void ApplyValidity(DateTime? expiryDate, string status)
        {
            if (!expiryDate.HasValue) return;

            lblValidUntil.Text = expiryDate.Value.ToString("dd MMMM yyyy", ZA);

            bool expired = expiryDate.Value.Date < DateTime.Today
                           && status.Equals("Sent", StringComparison.OrdinalIgnoreCase);
            if (expired)
            {
                lblValidUntil.Text += " (expired)";
                lblValidUntil.CssClass = "valid-expired fw-bold";
            }
        }

        private void ApplyStatusBadge(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return;

            string css;
            switch (status.Trim().ToLowerInvariant())
            {
                case "accepted": css = "badge bg-success"; break;
                case "rejected":
                case "declined": css = "badge bg-danger"; break;
                case "sent": css = "badge bg-primary"; break;
                default: css = "badge bg-secondary"; break;
            }

            lblStatus.CssClass = css + " fs-6 px-3 py-2";
            lblStatus.Text = "Status: " + Server.HtmlEncode(status);
            lblStatus.Visible = true;
        }

        private void ShowNotFound(string message)
        {
            pnlQuote.Visible = false;
            btnPrint.Visible = false;
            pnlNotFound.Visible = true;
            lblNotFound.Text = Server.HtmlEncode(message);
        }
    }
}