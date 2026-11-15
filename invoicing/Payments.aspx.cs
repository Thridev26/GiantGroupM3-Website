
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3.Invoicing
{
    public partial class Payments : Page
    {
        private string ConnectionString
        {
            get
            {
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings["DefaultConnection"];

                if (settings == null ||
                    string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new ConfigurationErrorsException(
                        "The DefaultConnection connection string is missing from Web.config.");
                }

                return settings.ConnectionString;
            }
        }

        private int ClientId
        {
            get
            {
                int clientId;

                return int.TryParse(
                    Convert.ToString(Session["ClientID"], CultureInfo.InvariantCulture),
                    out clientId)
                    ? clientId
                    : 0;
            }
        }

        // Staff who can see the Invoicing menu (every level except 1, 4 and 5) get a read-only
        // overview of all clients' payments. Making a payment stays a client-only action.
        private bool IsStaffViewer
        {
            get
            {
                if (!Request.IsAuthenticated || ClientId > 0)
                    return false;

                if (string.Equals(
                    Convert.ToString(Session["UserRole"]),
                    "Client",
                    StringComparison.OrdinalIgnoreCase))
                    return false;

                int level;

                return int.TryParse(
                    Convert.ToString(Session["UserLevel"], CultureInfo.InvariantCulture),
                    out level) &&
                    level >= 2 && level != 4 && level != 5;
            }
        }

        protected string PageHeading
        {
            get { return IsStaffViewer ? "Client Payments" : "My Payments"; }
        }

        protected string PageIntro
        {
            get
            {
                return IsStaffViewer
                    ? "Read-only overview of every client's job invoices, recorded payments and outstanding balances."
                    : "View your job invoices, recorded payments, and remaining balances. " +
                      "You can pay the full outstanding amount or make a partial payment.";
            }
        }

        protected bool ShowPayNote
        {
            get { return !IsStaffViewer; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // A signed-in client with a server-side ClientID, or an authorised staff member
            // (read-only), may view this page.
            bool isClient =
                Request.IsAuthenticated &&
                ClientId > 0 &&
                string.Equals(
                    Convert.ToString(Session["UserRole"]),
                    "Client",
                    StringComparison.OrdinalIgnoreCase);

            if (!isClient && !IsStaffViewer)
            {
                // Visitors who are not signed in go to the login page. A signed-in user who is
                // not allowed here is sent back to their own portal instead of the login page.
                Response.Redirect(
                    ResolveUrl(Request.IsAuthenticated
                        ? "~/HomeRouter.aspx"
                        : "~/Account/Login.aspx"),
                    false);

                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (IsStaffViewer)
                Page.Title = "Client Payments";

            if (!IsPostBack)
            {
                string paymentStatus = Request.QueryString["payment"];

                if (string.Equals(
                    paymentStatus, "return", StringComparison.OrdinalIgnoreCase))
                {
                    pnlPaymentMessage.Visible = true;
                    lblPaymentMessage.Text =
                        "You have returned from PayFast. Your payment will appear " +
                        "once PayFast has confirmed it. A receipt has been sent to yout email.";
                }
                else if (string.Equals(
                    paymentStatus, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    pnlPaymentMessage.Visible = true;
                    lblPaymentMessage.Text =
                        "The checkout was cancelled. You can try again whenever you are ready.";
                }

                LoadPayments();
            }
        }

        // Read-only list of every client's job invoices for authorised staff.
        private void LoadAllClientPayments()
        {
            const string sql = @"
                WITH PaymentTotals AS
                (
                    SELECT jobID, SUM(amountPaid) AS TotalPaid
                    FROM dbo.Payment
                    GROUP BY jobID
                )
                SELECT
                    j.jobID,
                    j.startDate,
                    j.jobStatus,
                    LTRIM(RTRIM(COALESCE(c.clientName, '') + ' ' + COALESCE(c.clientSurname, '')))
                        AS ClientName,
                    q.amount AS InvoiceTotal,
                    CAST(COALESCE(pt.TotalPaid, 0) AS decimal(18, 2)) AS TotalPaid,
                    CAST(q.amount - COALESCE(pt.TotalPaid, 0) AS decimal(18, 2))
                        AS BalanceOutstanding
                FROM dbo.Job AS j
                INNER JOIN dbo.Quote AS q
                    ON j.quoteID = q.QuoteID
                INNER JOIN dbo.JobRequest AS jr
                    ON q.jobRequestID = jr.jobRequestID
                LEFT JOIN dbo.Client AS c
                    ON jr.clientID = c.clientID
                LEFT JOIN PaymentTotals AS pt
                    ON j.jobID = pt.jobID
                ORDER BY j.jobID DESC;";

            try
            {
                var table = new DataTable();

                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    adapter.Fill(table);
                }

                bool hasReview = false;

                foreach (DataRow row in table.Rows)
                {
                    if (row["BalanceOutstanding"] != DBNull.Value &&
                        Convert.ToDecimal(
                            row["BalanceOutstanding"],
                            CultureInfo.InvariantCulture) < -0.01m)
                    {
                        hasReview = true;
                        break;
                    }
                }

                pnlReviewNote.Visible = hasReview;

                // Staff see who each invoice belongs to, and no payment controls.
                gvPayments.Columns.Insert(0, new BoundField
                {
                    DataField = "ClientName",
                    HeaderText = "Client"
                });

                foreach (DataControlField column in gvPayments.Columns)
                {
                    if (column.HeaderText == "Amount to Pay (R)" ||
                        column.HeaderText == "Action")
                    {
                        column.Visible = false;
                    }
                }

                gvPayments.EmptyDataText = "No job invoices were found.";
                gvPayments.DataSource = table;
                gvPayments.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Staff payments overview failed: {0}", ex);

                pnlError.Visible = true;
            }
        }

        private void LoadPayments()
        {
            if (IsStaffViewer)
            {
                LoadAllClientPayments();
                return;
            }

            const string sql = @"
                WITH PaymentTotals AS
                (
                    SELECT jobID, SUM(amountPaid) AS TotalPaid
                    FROM dbo.Payment
                    GROUP BY jobID
                )
                SELECT
                    j.jobID,
                    j.startDate,
                    j.jobStatus,
                    q.amount AS InvoiceTotal,
                    CAST(COALESCE(pt.TotalPaid, 0) AS decimal(18, 2)) AS TotalPaid,
                    CAST(q.amount - COALESCE(pt.TotalPaid, 0) AS decimal(18, 2))
                        AS BalanceOutstanding
                FROM dbo.Job AS j
                INNER JOIN dbo.Quote AS q
                    ON j.quoteID = q.QuoteID
                INNER JOIN dbo.JobRequest AS jr
                    ON q.jobRequestID = jr.jobRequestID
                LEFT JOIN PaymentTotals AS pt
                    ON j.jobID = pt.jobID
                WHERE jr.clientID = @clientID
                ORDER BY j.jobID DESC;";

            try
            {
                var table = new DataTable();

                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, conn))
                using (var adapter = new SqlDataAdapter(cmd))
                {
                    cmd.Parameters.Add("@clientID", SqlDbType.Int).Value = ClientId;
                    adapter.Fill(table);
                }

                bool hasReview = false;

                foreach (DataRow row in table.Rows)
                {
                    if (row["BalanceOutstanding"] != DBNull.Value &&
                        Convert.ToDecimal(
                            row["BalanceOutstanding"],
                            CultureInfo.InvariantCulture) < -0.01m)
                    {
                        hasReview = true;
                        break;
                    }
                }

                pnlReviewNote.Visible = hasReview;

                gvPayments.DataSource = table;
                gvPayments.DataBind();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Client payments page failed for ClientID {0}: {1}",
                    ClientId,
                    ex);

                pnlError.Visible = true;
            }
        }

        protected string FormatBalance(object value)
        {
            if (value == null || value == DBNull.Value)
                return "—";

            decimal balance = Convert.ToDecimal(
                value, CultureInfo.InvariantCulture);

            if (balance < -0.01m)
                return "Account review required";

            if (balance <= 0.01m)
                return "0.00";

            return balance.ToString(
                "N2", CultureInfo.GetCultureInfo("en-ZA"));
        }

        protected string FormatPaymentAmount(object value)
        {
            if (value == null || value == DBNull.Value)
                return string.Empty;

            decimal balance = Convert.ToDecimal(
                value, CultureInfo.InvariantCulture);

            if (balance < 5.00m)
                return string.Empty;

            return balance.ToString(
                "0.00", CultureInfo.InvariantCulture);
        }

        protected string AccountStatus(object value)
        {
            if (value == null || value == DBNull.Value)
                return "Check account";

            decimal balance = Convert.ToDecimal(
                value, CultureInfo.InvariantCulture);

            if (balance < -0.01m)
                return "Review required";

            return balance <= 0.01m ? "Paid" : "Outstanding";
        }

        protected void gvPayments_RowDataBound(
            object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow)
                return;

            object value = DataBinder.Eval(
                e.Row.DataItem, "BalanceOutstanding");

            if (value == null || value == DBNull.Value)
                return;

            decimal balance = Convert.ToDecimal(
                value, CultureInfo.InvariantCulture);

            if (balance < -0.01m)
            {
                e.Row.CssClass =
                    (e.Row.CssClass + " table-warning").Trim();
            }
            else if (balance > 0.01m)
            {
                e.Row.CssClass =
                    (e.Row.CssClass + " table-light").Trim();
            }
        }

        protected bool CanPay(object value)
        {
            if (value == null || value == DBNull.Value)
                return false;

            decimal balance = Convert.ToDecimal(
                value, CultureInfo.InvariantCulture);

            return balance >= 5.00m;
        }

        protected void gvPayments_RowCommand(
            object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "PayNow" || IsStaffViewer)
                return;

            pnlError.Visible = false;

            int jobId;

            if (!int.TryParse(
                Convert.ToString(e.CommandArgument, CultureInfo.InvariantCulture),
                out jobId) ||
                jobId <= 0)
            {
                pnlError.Visible = true;
                return;
            }

            // Find the amount entered for the selected job.
            GridViewRow selectedRow = null;

            foreach (GridViewRow row in gvPayments.Rows)
            {
                Button button = row.FindControl("btnPayNow") as Button;

                if (button != null &&
                    string.Equals(
                        button.CommandArgument,
                        jobId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal))
                {
                    selectedRow = row;
                    break;
                }
            }

            if (selectedRow == null)
            {
                pnlError.Visible = true;
                return;
            }

            TextBox txtPaymentAmount =
                selectedRow.FindControl("txtPaymentAmount") as TextBox;

            decimal paymentAmount;

            if (txtPaymentAmount == null ||
                !decimal.TryParse(
                    txtPaymentAmount.Text,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out paymentAmount) ||
                paymentAmount < 5.00m ||
                decimal.Round(paymentAmount, 2) != paymentAmount)
            {
                pnlError.Visible = true;
                pnlPaymentMessage.Visible = true;
                lblPaymentMessage.Text =
                    "Enter a valid payment amount of at least R5.00, " +
                    "using no more than two decimal places.";
                return;
            }

            // The checkout handler validates the job belongs to this client
            // and checks the entered amount against the current balance.
            string checkoutUrl =
                ResolveUrl("~/PayFastCheckout.ashx") +
                "?jobId=" + HttpUtility.UrlEncode(
                    jobId.ToString(CultureInfo.InvariantCulture)) +
                "&amount=" + HttpUtility.UrlEncode(
                    paymentAmount.ToString("0.00", CultureInfo.InvariantCulture));

            Response.Redirect(checkoutUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
