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
                return int.TryParse(Convert.ToString(Session["ClientID"], CultureInfo.InvariantCulture), out clientId) ? clientId : 0;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            // Only a signed-in client with a server-side ClientID may view this page.
            if (!Request.IsAuthenticated || ClientId <= 0 ||
                !string.Equals(Convert.ToString(Session["UserRole"]), "Client", StringComparison.OrdinalIgnoreCase))
            {
                Response.Redirect(ResolveUrl("~/Account/Login.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack) LoadPayments();
        }

        private void LoadPayments()
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
                    q.amount AS InvoiceTotal,
                    CAST(COALESCE(pt.TotalPaid, 0) AS decimal(18, 2)) AS TotalPaid,
                    CAST(q.amount - COALESCE(pt.TotalPaid, 0) AS decimal(18, 2)) AS BalanceOutstanding
                FROM dbo.Job AS j
                INNER JOIN dbo.Quote AS q ON j.quoteID = q.QuoteID
                INNER JOIN dbo.JobRequest AS jr ON q.jobRequestID = jr.jobRequestID
                LEFT JOIN PaymentTotals AS pt ON j.jobID = pt.jobID
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
                    if (row["BalanceOutstanding"] != DBNull.Value && Convert.ToDecimal(row["BalanceOutstanding"]) < -0.01m)
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
                System.Diagnostics.Trace.TraceError("Client payments page failed for ClientID {0}: {1}", ClientId, ex);
                pnlError.Visible = true;
            }
        }

        protected string FormatBalance(object value)
        {
            if (value == null || value == DBNull.Value) return "—";
            decimal balance = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (balance < -0.01m) return "Account review required";
            if (balance <= 0.01m) return "0.00";
            return balance.ToString("N2", CultureInfo.GetCultureInfo("en-ZA"));
        }

        protected string AccountStatus(object value)
        {
            if (value == null || value == DBNull.Value) return "Check account";
            decimal balance = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (balance < -0.01m) return "Review required";
            return balance <= 0.01m ? "Paid" : "Outstanding";
        }

        protected void gvPayments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            object value = DataBinder.Eval(e.Row.DataItem, "BalanceOutstanding");
            if (value == null || value == DBNull.Value) return;

            decimal balance = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (balance < -0.01m)
            {
                e.Row.CssClass = (e.Row.CssClass + " table-warning").Trim();
            }
            else if (balance > 0.01m)
            {
                e.Row.CssClass = (e.Row.CssClass + " table-light").Trim();
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
            if (e.CommandName != "PayNow")
                return;

            int jobId;

            if (!int.TryParse(
                Convert.ToString(e.CommandArgument), out jobId)
                || jobId <= 0)
            {
                pnlError.Visible = true;
                return;
            }

            Response.Redirect(
                ResolveUrl("~/PayFastCheckout.ashx?jobId=" + jobId),
                false);

            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
