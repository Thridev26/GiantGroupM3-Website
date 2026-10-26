using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class ViewQuote : System.Web.UI.Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        private enum AccessRole { None, Management, Client }
        private AccessRole _role = AccessRole.None;
        private int _clientId;

        // Statuses a manager may set, and the filter options each role is offered.
        private static readonly string[] EditableStatuses = { "Pending", "Sent", "Accepted", "Declined", "Rejected" };
        private static readonly string[] ManagementFilters = { "All", "Pending", "Sent", "Accepted", "Declined", "Rejected" };
        private static readonly string[] ClientFilters = { "All", "Sent", "Accepted", "Rejected" };

        /// <summary>
        /// The selected quote lives in ViewState (MAC-protected), NOT in a HiddenField.
        /// Hidden fields are posted by the browser and can be edited by the user.
        /// </summary>
        private int SelectedQuoteId
        {
            get { return ViewState["SelectedQuoteId"] is int id ? id : 0; }
            set { ViewState["SelectedQuoteId"] = value; }
        }

        // ------------------------------------------------------------------
        //  Page lifecycle + access control
        // ------------------------------------------------------------------
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // Checked on EVERY request (not just first load) so postbacks can't skip it.
            if (!ResolveAccess())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                lblSubtitle.Text = IsClientView
                    ? "Your quotations. Pick a status to see just those quotes and their totals."
                    : "Review, manage, and track client quotations. The cards follow the status you pick.";
                PopulateFilter();
                LoadQuoteData();
            }
        }

        /// <summary>
        /// Fail closed: only Owner/Admin (UserLevel 5 or 6) get management rights, only a real ClientID gets client rights.
        /// Previously any authenticated user without a client session was treated as management.
        /// </summary>
        private bool ResolveAccess()
        {
            _role = AccessRole.None;
            _clientId = 0;

            if (Session["UserLevel"] != null
                && int.TryParse(Session["UserLevel"].ToString(), out int level)
                && (level == 5 || level == 6))
            {
                _role = AccessRole.Management;
                return true;
            }

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

        private bool IsClientView { get { return _role == AccessRole.Client; } }
        private bool IsManagementView { get { return _role == AccessRole.Management; } }

        // ------------------------------------------------------------------
        //  Data binding helpers used by the .aspx
        // ------------------------------------------------------------------
        protected string SelectButtonText()
        {
            return IsClientView ? "Review" : "Select/Edit";
        }

        protected string GetStatusCss(object status)
        {
            string s = Convert.ToString(status)?.Trim().ToLowerInvariant() ?? string.Empty;
            switch (s)
            {
                case "accepted": return "badge rounded-pill bg-success";
                case "rejected":
                case "declined": return "badge rounded-pill bg-danger";
                case "sent": return "badge rounded-pill bg-primary";
                default: return "badge rounded-pill bg-secondary";
            }
        }

        protected string GetExpiryHtml(object expiry, object status)
        {
            if (expiry == null || expiry == DBNull.Value) return "<span class=\"text-muted\">-</span>";

            DateTime date = Convert.ToDateTime(expiry);
            string text = Server.HtmlEncode(date.ToString("yyyy-MM-dd"));
            bool stillOpen = string.Equals(Convert.ToString(status), "Sent", StringComparison.OrdinalIgnoreCase);

            if (stillOpen && date.Date < DateTime.Today)
                return text + " <span class=\"badge bg-danger-subtle text-danger-emphasis ms-1\">Expired</span>";

            return text;
        }

        // ------------------------------------------------------------------
        //  Listing
        // ------------------------------------------------------------------
        private void PopulateFilter()
        {
            ddlStatusFilter.Items.Clear();
            foreach (string s in IsClientView ? ClientFilters : ManagementFilters)
                ddlStatusFilter.Items.Add(new ListItem(s == "All" ? "All statuses" : s, s));
        }

        private void LoadQuoteData()
        {
            string query = @"SELECT q.QuoteID, q.jobRequestID, c.clientName AS Name, q.dateIssued, q.expiryDate, q.dateGenerated, q.amount, q.quoteStatus, q.filePath
                             FROM Quote q
                             JOIN JobRequest jr ON q.jobRequestID = jr.jobRequestID
                             JOIN Client c ON jr.clientID = c.clientID ";

            if (IsClientView)
            {
                // Clients only ever see their own quotes, and only ones that were actually sent to them.
                query += "WHERE q.quoteStatus IN ('Sent', 'Accepted', 'Rejected') AND c.clientID = @clientID ";
            }
            // Management sees every status (so a quote set to Pending/Declined doesn't silently vanish).

            query += "ORDER BY q.dateIssued DESC, q.QuoteID DESC";

            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (IsClientView)
                    cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = _clientId;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            BindStats(dt, SelectedFilter());

            bool hasAny = dt.Rows.Count > 0;
            pnlEmptyState.Visible = !hasAny;
            pnlStats.Visible = hasAny;
            divTableContainer.Visible = hasAny;
            ddlStatusFilter.Visible = hasAny;
            lblStatusFilter.Visible = hasAny;

            if (!hasAny) return;

            // In-memory filter. The value is checked against a fixed whitelist before it touches RowFilter.
            DataView view = dt.DefaultView;
            string filter = SelectedFilter();
            if (filter != "All")
                view.RowFilter = "quoteStatus = '" + filter + "'";

            gvQuotes.DataSource = view;
            gvQuotes.DataBind();
        }

        /// <summary>The chosen status filter, checked against the fixed whitelist ("All" when invalid).</summary>
        private string SelectedFilter()
        {
            string filter = ddlStatusFilter.SelectedValue;
            string[] allowed = IsClientView ? ClientFilters : ManagementFilters;
            return (!string.IsNullOrEmpty(filter) && Array.IndexOf(allowed, filter) >= 0) ? filter : "All";
        }

        /// <summary>
        /// Summary cards. "All" shows the overall picture (total / awaiting / accepted / accepted value).
        /// Picking a status re-points every card at that status. dt only ever holds the signed-in
        /// client's own quotes (or everything for management), so a client never sees anyone else's figures.
        /// </summary>
        private void BindStats(DataTable dt, string filter)
        {
            int total = dt.Rows.Count;

            if (filter == "All")
            {
                int awaiting = 0, accepted = 0;
                decimal acceptedValue = 0m;
                foreach (DataRow row in dt.Rows)
                {
                    string status = (Convert.ToString(row["quoteStatus"]) ?? string.Empty).Trim();
                    if (status.Equals("Sent", StringComparison.OrdinalIgnoreCase)) awaiting++;
                    else if (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        accepted++;
                        if (row["amount"] != DBNull.Value) acceptedValue += Convert.ToDecimal(row["amount"]);
                    }
                }

                lblStatTotalTitle.Text = "Total quotes";
                lblStatAwaitingTitle.Text = "Awaiting response";
                lblStatAcceptedTitle.Text = "Accepted";
                lblStatValueTitle.Text = "Accepted value";
                lblStatTotal.Text = total.ToString();
                lblStatAwaiting.Text = awaiting.ToString();
                lblStatAccepted.Text = accepted.ToString();
                lblStatValue.Text = acceptedValue.ToString("C");
                return;
            }

            int count = 0;
            decimal value = 0m;
            foreach (DataRow row in dt.Rows)
            {
                string status = (Convert.ToString(row["quoteStatus"]) ?? string.Empty).Trim();
                if (!status.Equals(filter, StringComparison.OrdinalIgnoreCase)) continue;
                count++;
                if (row["amount"] != DBNull.Value) value += Convert.ToDecimal(row["amount"]);
            }

            lblStatTotalTitle.Text = filter + " quotes";
            lblStatAwaitingTitle.Text = filter + " value";
            lblStatAcceptedTitle.Text = "Average value";
            lblStatValueTitle.Text = "Share of all quotes";
            lblStatTotal.Text = count.ToString();
            lblStatAwaiting.Text = value.ToString("C");
            lblStatAccepted.Text = (count > 0 ? value / count : 0m).ToString("C");
            lblStatValue.Text = total > 0 ? ((double)count / total).ToString("P0") : "0%";
        }

        protected void ddlStatusFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            gvQuotes.PageIndex = 0;
            ClearSelection();
            LoadQuoteData();
        }

        protected void gvQuotes_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvQuotes.PageIndex = e.NewPageIndex;
            ClearSelection();
            LoadQuoteData();
        }

        // ------------------------------------------------------------------
        //  Selecting a quote
        // ------------------------------------------------------------------
        protected void gvQuotes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_role == AccessRole.None) return;

            int quoteId = Convert.ToInt32(gvQuotes.SelectedDataKey.Value);
            ShowQuote(quoteId);
        }

        private void ShowQuote(int quoteId)
        {
            pnlMessage.Visible = false;
            pnlEditError.Visible = false;

            // Re-check ownership in SQL: never trust that the id is one this user is allowed to see.
            const string query = @"SELECT q.QuoteID, q.jobRequestID, c.clientName, q.dateIssued, q.expiryDate, q.amount, q.quoteStatus
                                   FROM Quote q
                                   JOIN JobRequest jr ON q.jobRequestID = jr.jobRequestID
                                   JOIN Client c ON jr.clientID = c.clientID
                                   WHERE q.QuoteID = @QuoteID
                                     AND (@IsStaff = 1
                                          OR (c.clientID = @ClientID AND q.quoteStatus IN ('Sent', 'Accepted', 'Rejected')))";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                cmd.Parameters.Add("@IsStaff", SqlDbType.Bit).Value = IsManagementView;
                cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = _clientId;
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        ClearSelection();
                        ShowMessage("That quote could not be found.", false);
                        return;
                    }

                    SelectedQuoteId = quoteId;

                    int jobId = Convert.ToInt32(reader["jobRequestID"]);
                    string status = Convert.ToString(reader["quoteStatus"]) ?? string.Empty;
                    decimal amount = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0m;
                    DateTime? expiry = reader["expiryDate"] != DBNull.Value ? Convert.ToDateTime(reader["expiryDate"]) : (DateTime?)null;
                    DateTime? issued = reader["dateIssued"] != DBNull.Value ? Convert.ToDateTime(reader["dateIssued"]) : (DateTime?)null;

                    // Read-only summary
                    lblDetailTitle.Text = Server.HtmlEncode($"Quote GT – {jobId}");
                    lblDetailClient.Text = Server.HtmlEncode(Convert.ToString(reader["clientName"]));
                    lblDetailIssued.Text = issued.HasValue ? issued.Value.ToString("yyyy-MM-dd") : "-";
                    lblDetailExpiry.Text = expiry.HasValue ? expiry.Value.ToString("yyyy-MM-dd") : "-";
                    lblDetailAmount.Text = amount.ToString("C");
                    lblDetailStatus.Text = Server.HtmlEncode(status);
                    lblDetailStatus.CssClass = GetStatusCss(status);
                    lnkDetailPrint.NavigateUrl = "~/PrintQuote.aspx?jobRequestID=" + jobId;

                    pnlEditQuote.Visible = true;
                    pnlManagementControls.Visible = IsManagementView;

                    if (IsManagementView)
                    {
                        txtExpiryDate.Text = expiry.HasValue ? expiry.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
                        txtAmount.Text = amount.ToString("0.00", CultureInfo.InvariantCulture);

                        ListItem item = ddlQuoteStatus.Items.FindByValue(status);
                        ddlQuoteStatus.ClearSelection();
                        if (item != null) item.Selected = true;
                    }

                    // Clients can only answer a quote that is still 'Sent' and not past its expiry date.
                    bool isOpen = status.Equals("Sent", StringComparison.OrdinalIgnoreCase);
                    bool expired = expiry.HasValue && expiry.Value.Date < DateTime.Today;

                    pnlClientResponse.Visible = IsClientView && isOpen && !expired;
                    pnlClientReadOnly.Visible = IsClientView && !pnlClientResponse.Visible;

                    if (pnlClientReadOnly.Visible)
                    {
                        lblClientReadOnly.Text = !isOpen
                            ? Server.HtmlEncode($"This quote has already been responded to (status: {status}).")
                            : Server.HtmlEncode("This quote has expired. Please contact us if you would still like to go ahead.");
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        //  Management: edit quote
        // ------------------------------------------------------------------
        protected void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!IsManagementView) return;

            int quoteId = SelectedQuoteId;
            if (quoteId == 0) return;

            // ---- Validate input (previously Convert.* would throw on bad text) ----
            if (!DateTime.TryParseExact(txtExpiryDate.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime expiryDate))
            {
                ShowEditError("Enter a valid expiry date.");
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount)
                || amount <= 0m || amount > 100000000m)
            {
                ShowEditError("Enter a valid amount greater than zero.");
                return;
            }
            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

            string newStatus = ddlQuoteStatus.SelectedValue;
            if (Array.IndexOf(EditableStatuses, newStatus) < 0)
            {
                ShowEditError("Choose a valid status.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            string oldStatus;
                            using (SqlCommand read = new SqlCommand("SELECT quoteStatus FROM Quote WITH (UPDLOCK) WHERE QuoteID = @QuoteID", conn, tx))
                            {
                                read.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                                object o = read.ExecuteScalar();
                                if (o == null) { tx.Rollback(); ShowEditError("That quote no longer exists."); return; }
                                oldStatus = Convert.ToString(o);
                            }

                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE Quote SET expiryDate = @expiryDate, amount = @amount, quoteStatus = @quoteStatus WHERE QuoteID = @QuoteID", conn, tx))
                            {
                                cmd.Parameters.Add("@expiryDate", SqlDbType.DateTime).Value = expiryDate;
                                SqlParameter amt = cmd.Parameters.Add("@amount", SqlDbType.Decimal);
                                amt.Precision = 18; amt.Scale = 2; amt.Value = amount;
                                cmd.Parameters.Add("@quoteStatus", SqlDbType.NVarChar, 50).Value = newStatus;
                                cmd.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                                cmd.ExecuteNonQuery();
                            }

                            // Keep the job in step when management changes the outcome - but only when the status
                            // actually changed, so editing an expiry date later can't drag a job backwards.
                            string jobStatus = MapJobStatus(newStatus);
                            if (jobStatus != null && !string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                            {
                                using (SqlCommand cmdJob = new SqlCommand(
                                    @"UPDATE JobRequest SET status = @jobStatus
                                      WHERE jobRequestID = (SELECT jobRequestID FROM Quote WHERE QuoteID = @QuoteID)", conn, tx))
                                {
                                    cmdJob.Parameters.Add("@jobStatus", SqlDbType.NVarChar, 50).Value = jobStatus;
                                    cmdJob.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                                    cmdJob.ExecuteNonQuery();
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ViewQuote update error: " + ex);
                ShowEditError("We couldn't save your changes. Please try again.");
                return;
            }

            ClearSelection();
            ShowMessage("Quote updated successfully!", true);
            LoadQuoteData();
        }

        // ------------------------------------------------------------------
        //  Client: accept / reject
        // ------------------------------------------------------------------
        protected void btnAcceptQuote_Click(object sender, EventArgs e)
        {
            UpdateQuoteStatusAsClient("Accepted", "Quote successfully accepted!");
        }

        protected void btnRejectQuote_Click(object sender, EventArgs e)
        {
            UpdateQuoteStatusAsClient("Rejected", "Quote has been rejected.");
        }

        private void UpdateQuoteStatusAsClient(string newQuoteStatus, string successMessage)
        {
            // Only a signed-in client may respond, and only to their own quote.
            if (!IsClientView) return;

            int quoteId = SelectedQuoteId;
            if (quoteId == 0) return;

            string jobStatusToSet = MapJobStatus(newQuoteStatus);

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        try
                        {
                            // Ownership + state check in the UPDATE itself: must be this client's quote,
                            // still 'Sent', and not past its expiry date. 0 rows = refuse.
                            const string updateQuote = @"
                                UPDATE q SET q.quoteStatus = @quoteStatus
                                FROM Quote q
                                JOIN JobRequest jr ON jr.jobRequestID = q.jobRequestID
                                WHERE q.QuoteID = @QuoteID
                                  AND jr.clientID = @ClientID
                                  AND q.quoteStatus = 'Sent'
                                  AND (q.expiryDate IS NULL OR q.expiryDate >= CAST(GETDATE() AS date))";

                            int changed;
                            using (SqlCommand cmdQuote = new SqlCommand(updateQuote, conn, tx))
                            {
                                cmdQuote.Parameters.Add("@quoteStatus", SqlDbType.NVarChar, 50).Value = newQuoteStatus;
                                cmdQuote.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                                cmdQuote.Parameters.Add("@ClientID", SqlDbType.Int).Value = _clientId;
                                changed = cmdQuote.ExecuteNonQuery();
                            }

                            if (changed != 1)
                            {
                                tx.Rollback();
                                ClearSelection();
                                ShowMessage("This quote can no longer be responded to. It may have expired or already been answered.", false);
                                LoadQuoteData();
                                return;
                            }

                            using (SqlCommand cmdJob = new SqlCommand(
                                @"UPDATE JobRequest SET status = @jobStatus
                                  WHERE jobRequestID = (SELECT jobRequestID FROM Quote WHERE QuoteID = @QuoteID)", conn, tx))
                            {
                                cmdJob.Parameters.Add("@jobStatus", SqlDbType.NVarChar, 50).Value = jobStatusToSet;
                                cmdJob.Parameters.Add("@QuoteID", SqlDbType.Int).Value = quoteId;
                                cmdJob.ExecuteNonQuery();
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ViewQuote respond error: " + ex);
                ShowMessage("We couldn't record your response. Please try again.", false);
                return;
            }

            ClearSelection();
            ShowMessage(successMessage, true);
            LoadQuoteData();
        }

        /// <summary>Maps a quote status onto the JobRequest CHECK-constraint values already used in the system.</summary>
        private static string MapJobStatus(string quoteStatus)
        {
            if (quoteStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase)) return "Quote Accepted";
            if (quoteStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
            if (quoteStatus.Equals("Declined", StringComparison.OrdinalIgnoreCase)) return "Cancelled";
            if (quoteStatus.Equals("Sent", StringComparison.OrdinalIgnoreCase)) return "Quote Sent";
            return null; // Pending: leave the job alone
        }

        // ------------------------------------------------------------------
        //  Cancel / UI helpers
        // ------------------------------------------------------------------
        protected void btnCancel_Click(object sender, EventArgs e)
        {
            ClearSelection();
            pnlMessage.Visible = false;
            lblMessage.Text = string.Empty;
        }

        private void ClearSelection()
        {
            pnlEditQuote.Visible = false;
            pnlEditError.Visible = false;
            gvQuotes.SelectedIndex = -1;
            SelectedQuoteId = 0;
        }

        private void ShowMessage(string text, bool success)
        {
            lblMessage.Text = Server.HtmlEncode(text);
            pnlMessage.CssClass = success ? "alert alert-success rounded-3 mb-3" : "alert alert-danger rounded-3 mb-3";
            pnlMessage.Visible = true;
        }

        private void ShowEditError(string text)
        {
            lblEditError.Text = Server.HtmlEncode(text);
            pnlEditError.Visible = true;
        }
    }
}