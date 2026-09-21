using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class RequestQuote : Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["defaultConnection"].ConnectionString;

        // Hardcoded Business Location Constants from your frontend logic
        private const double BaseLatitude = -29.890840081918007;
        private const double BaseLongitude = 30.905937134956915;
        private const decimal CostPerKilometer = 36.00m;
        private const decimal BaseCallOutFee = 500.00m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Session["ClientID"] != null && int.TryParse(Session["ClientID"].ToString(), out int clientId))
                {
                    BindExistingJobRequests(clientId);
                }
                else
                {
                    Response.Redirect("~/Login.aspx");
                }
            }
        }

        private void BindExistingJobRequests(int clientId)
        {
            string query = "SELECT jobRequestID, siteAddress, urgencyLevel, dateRecieved, status " +
                           "FROM JobRequest " +
                           "WHERE clientID = @ClientID " +
                           "AND status NOT IN ('Cancelled', 'Quote Sent', 'Quote Accepted')";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ClientID", clientId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvJobRequests.DataSource = dt;
                        gvJobRequests.DataBind();
                    }
                }
            }
        }

        protected void gvJobRequests_SelectedIndexChanged(object sender, EventArgs e)
        {
            GridViewRow row = gvJobRequests.SelectedRow;
            int jobRequestId = Convert.ToInt32(gvJobRequests.DataKeys[row.RowIndex].Value);
            txtSelectedJobId.Text = jobRequestId.ToString();

            LoadJobRequestDetails(jobRequestId);
            BindRequestItems(jobRequestId);
            CalculateQuoteTotals(jobRequestId);
        }

        private void LoadJobRequestDetails(int jobRequestId)
        {
            string query = "SELECT longitude, latitude FROM JobRequest WHERE jobRequestID = @JobRequestID";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtLongitude.Text = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]).ToString("0.######") : "0";
                            txtLatitude.Text = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]).ToString("0.######") : "0";
                        }
                    }
                }
            }
        }

        private void BindRequestItems(int jobRequestId)
        {
            string query = "SELECT jt.JobTypeName, jt.JobRate, ISNULL(id.detailValue, 1) AS Quantity, " +
                           "(jt.JobRate * ISNULL(id.detailValue, 1)) AS LineTotal " +
                           "FROM RequestItem ri " +
                           "INNER JOIN JobType jt ON ri.jobTypeID = jt.jobTypeID " +
                           "LEFT JOIN ItemDetail id ON ri.requestItemID = id.requestItemID " +
                           "WHERE ri.jobRequestID = @JobRequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        gvRequestItems.DataSource = dt;
                        gvRequestItems.DataBind();
                    }
                }
            }
        }

        private decimal CalculateTravelFee(double clientLat, double clientLng)
        {
            if (clientLat == 0.0 || clientLng == 0.0)
            {
                decimal flatFallbackFee = 850.00m;
                return flatFallbackFee;
            }

            double dLat = (clientLat - BaseLatitude) * (Math.PI / 180.0);
            double dLng = (clientLng - BaseLongitude) * (Math.PI / 180.0);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(BaseLatitude * (Math.PI / 180.0)) * Math.Cos(clientLat * (Math.PI / 180.0)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distanceInKm = 6371.0 * c;

            decimal transportCost = BaseCallOutFee + (Convert.ToDecimal(distanceInKm) * 2 * CostPerKilometer);

            return Math.Round(transportCost, 2);
        }

        private void CalculateQuoteTotals(int jobRequestId)
        {
            decimal servicesSubtotal = 0;
            string calcQuery = "SELECT ISNULL(SUM(jt.JobRate * ISNULL(id.detailValue, 1)), 0) AS TotalRate " +
                               "FROM RequestItem ri " +
                               "INNER JOIN JobType jt ON ri.jobTypeID = jt.jobTypeID " +
                               "LEFT JOIN ItemDetail id ON ri.requestItemID = id.requestItemID " +
                               "WHERE ri.jobRequestID = @JobRequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(calcQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    servicesSubtotal = (decimal)cmd.ExecuteScalar();
                }
            }

            double lon = string.IsNullOrEmpty(txtLongitude.Text) ? 0.0 : Convert.ToDouble(txtLongitude.Text);
            double lat = string.IsNullOrEmpty(txtLatitude.Text) ? 0.0 : Convert.ToDouble(txtLatitude.Text);

            decimal travelFee = CalculateTravelFee(lat, lon);
            decimal subtotalWithTravel = servicesSubtotal + travelFee;
            decimal vat = subtotalWithTravel * 0.15m;
            decimal totalWithVat = subtotalWithTravel + vat;

            // Bind values to UI labels
            lblSubtotal.Text = servicesSubtotal.ToString("C");
            lblTravelFee.Text = travelFee.ToString("C");
            lblSubtotalWithTravel.Text = subtotalWithTravel.ToString("C");
            lblVat.Text = vat.ToString("C");
            lblTotalWithVat.Text = totalWithVat.ToString("C");
        }

        protected void btnGenerateArbitraryQuote_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSelectedJobId.Text))
            {
                lblMessage.Text = "Please select an existing Job Request first.";
                pnlMessage.CssClass = "alert alert-warning rounded-3 mb-4";
                pnlMessage.Visible = true;
                return;
            }

            int jobRequestId = Convert.ToInt32(txtSelectedJobId.Text);

            // Check if item details exist for this job request
            bool hasDetails = false;
            string checkQuery = "SELECT COUNT(*) FROM RequestItem ri INNER JOIN ItemDetail id ON ri.requestItemID = id.requestItemID WHERE ri.jobRequestID = @JobRequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    int count = (int)cmd.ExecuteScalar();
                    hasDetails = (count > 0);
                }
            }

            // If no item details exist, block quote generation and show disclaimer
            if (!hasDetails)
            {
                lblMessage.Text = "Cannot generate a preliminary quote yet. Site measurements or item details have not been provided for this job request, so costs cannot be realistically calculated.";
                pnlMessage.CssClass = "alert alert-danger rounded-3 mb-4";
                pnlMessage.Visible = true;
                btnViewPdfQuote.Visible = false;
                return;
            }

            // Otherwise, proceed successfully
            lblMessage.Text = "Cost preview accepted! You can now view your preliminary quote document below.";
            pnlMessage.CssClass = "alert alert-success rounded-3 mb-4";
            pnlMessage.Visible = true;

            // Reveals the view document button
            btnViewPdfQuote.Visible = true;
        }

        protected void btnViewPdfQuote_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSelectedJobId.Text))
            {
                string url = $"ViewQuoteDoc.aspx?jobRequestID={txtSelectedJobId.Text}";
                ClientScript.RegisterStartupScript(this.GetType(), "openWindow", $"window.open('{url}', '_blank');", true);
            }
        }
    }
}