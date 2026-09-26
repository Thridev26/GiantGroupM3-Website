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

        // Hardcoded Business Location Constants
        private const double BaseLatitude = -29.890840081918007;
        private const double BaseLongitude = 30.905937134956915;
        private const decimal CostPerKilometer = 36.00m;
        private const decimal BaseCallOutFee = 500.00m;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // 1. Check if an Owner (6) or Admin (5) is logged in
                if (Session["UserLevel"] != null && int.TryParse(Session["UserLevel"].ToString(), out int level) && (level == 5 || level == 6))
                {
                    BindAllExistingJobRequestsForManagement();
                }
                // 2. Check if a standard Client is logged in
                else if (Session["ClientID"] != null && int.TryParse(Session["ClientID"].ToString(), out int clientId))
                {
                    BindExistingJobRequestsForClient(clientId);
                }
                // 3. Otherwise, send them to login
                else
                {
                    Response.Redirect("~/Account/Login.aspx");
                }
            }
        }

        private void BindAllExistingJobRequestsForManagement()
        {
            string query = "SELECT jobRequestID, siteAddress, urgencyLevel, dateRecieved, status " +
                           "FROM JobRequest " +
                           "WHERE status NOT IN ('Cancelled', 'Quote Sent', 'Quote Accepted')";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                ExecuteJobRequestQuery(cmd);
            }
        }

        private void BindExistingJobRequestsForClient(int clientId)
        {
            string query = "SELECT jobRequestID, siteAddress, urgencyLevel, dateRecieved, status " +
                           "FROM JobRequest " +
                           "WHERE clientID = @ClientID " +
                           "AND status NOT IN ('Cancelled', 'Quote Sent', 'Quote Accepted')";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ClientID", clientId);
                ExecuteJobRequestQuery(cmd);
            }
        }

        private void ExecuteJobRequestQuery(SqlCommand cmd)
        {
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

        protected void gvJobRequests_SelectedIndexChanged(object sender, EventArgs e)
        {
            GridViewRow row = gvJobRequests.SelectedRow;
            int jobRequestId = Convert.ToInt32(gvJobRequests.DataKeys[row.RowIndex].Value);
            txtSelectedJobId.Text = jobRequestId.ToString();

            // 1. Show the selection details & hide the initial prompt
            pnlSelectionDetails.Visible = true;
            pnlSelectionPrompt.Visible = false;

            // 2. Show the lower quote sections (2, 3, and 4)
            pnlFormContent.Visible = true;
            pnlMessage.Visible = false;

            // 3. Load details and calculate totals
            LoadJobRequestDetails(jobRequestId);
            BindRequestItems(jobRequestId);
            CalculateQuoteTotals(jobRequestId);
        }

        private void LoadJobRequestDetails(int jobRequestId)
        {
            string query = "SELECT siteAddress, longitude, latitude FROM JobRequest WHERE jobRequestID = @JobRequestID";
            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string address = reader["siteAddress"] != DBNull.Value ? reader["siteAddress"].ToString() : string.Empty;

                        txtSiteAddress.Text = address;
                        lblDisplaySiteAddress.Text = string.IsNullOrWhiteSpace(address) ? "No address specified" : address;

                        hfLongitude.Value = reader["longitude"] != DBNull.Value ? Convert.ToDecimal(reader["longitude"]).ToString("0.######") : "0";
                        hfLatitude.Value = reader["latitude"] != DBNull.Value ? Convert.ToDecimal(reader["latitude"]).ToString("0.######") : "0";
                    }
                }
            }
        }
        
        private void BindRequestItems(int jobRequestId)
        {
            string query = @"
                SELECT 
                    jt.JobTypeName, 
                    jt.JobRate, 
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

            using (SqlConnection conn = new SqlConnection(connString))
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

        private decimal CalculateTravelFee(double clientLat, double clientLng)
        {
            // Sync display label with the loaded site address
            if (!string.IsNullOrWhiteSpace(txtSiteAddress.Text))
            {
                lblDisplaySiteAddress.Text = txtSiteAddress.Text;
            }

            if (clientLat == 0.0 || clientLng == 0.0)
            {
                lblOneWayDistance.Text = "0.0 km";
                lblTravelBreakdown.Text = "Standard flat rate travel fee applied (Coordinates not specified).";
                return 850.00m;
            }

            // Haversine formula using BaseLatitude & BaseLongitude
            double dLat = (clientLat - BaseLatitude) * (Math.PI / 180.0);
            double dLng = (clientLng - BaseLongitude) * (Math.PI / 180.0);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(BaseLatitude * (Math.PI / 180.0)) * Math.Cos(clientLat * (Math.PI / 180.0)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double oneWayDistance = 6371.0 * c; // Earth's radius ~6371 km

            decimal roundTripKm = Convert.ToDecimal(oneWayDistance) * 2;
            decimal distanceCost = roundTripKm * CostPerKilometer;
            decimal totalTransportCost = BaseCallOutFee + distanceCost;

            // Display one-way distance in route banner
            lblOneWayDistance.Text = $"{oneWayDistance:N1} km";

            lblTravelBreakdown.Text = $"Base Call-out: {BaseCallOutFee:C} + Round-trip ({roundTripKm:N1} km @ {CostPerKilometer:C}/km = {distanceCost:C})";

            return Math.Round(totalTransportCost, 2);
        }

        private void CalculateQuoteTotals(int jobRequestId)
        {
            decimal servicesSubtotal = GetServicesSubtotal(jobRequestId);

            double lat = string.IsNullOrEmpty(hfLatitude.Value) ? 0.0 : Convert.ToDouble(hfLatitude.Value);
            double lon = string.IsNullOrEmpty(hfLongitude.Value) ? 0.0 : Convert.ToDouble(hfLongitude.Value);

            decimal travelFee = CalculateTravelFee(lat, lon);
            decimal subtotalWithTravel = servicesSubtotal + travelFee;
            decimal vat = subtotalWithTravel * 0.15m;
            decimal totalWithVat = subtotalWithTravel + vat;

            lblSubtotal.Text = servicesSubtotal.ToString("C");
            lblTravelFee.Text = travelFee.ToString("C");
            lblTravelFeeSummary.Text = travelFee.ToString("C");
            lblSubtotalWithTravel.Text = subtotalWithTravel.ToString("C");
            lblVat.Text = vat.ToString("C");
            lblTotalWithVat.Text = totalWithVat.ToString("C");
        }

        private decimal GetServicesSubtotal(int jobRequestId)
        {
            string calcQuery = @"SELECT ISNULL(SUM(jt.JobRate * ISNULL(id.detailValue, 1)), 0) 
                                 FROM RequestItem ri 
                                 INNER JOIN JobType jt ON ri.jobTypeID = jt.jobTypeID 
                                 LEFT JOIN ItemDetail id ON ri.requestItemID = id.requestItemID 
                                 WHERE ri.jobRequestID = @JobRequestID";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(calcQuery, conn))
            {
                cmd.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
            }
        }

        protected void btnGenerateArbitraryQuote_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSelectedJobId.Text))
            {
                ShowMessage("Please select an existing Job Request first.", "alert alert-warning rounded-3 mb-4", false);
                return;
            }

            int jobRequestId = Convert.ToInt32(txtSelectedJobId.Text);

            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                // 1. Check if item details/measurements exist
                string checkDetailsQuery = "SELECT COUNT(*) FROM RequestItem ri INNER JOIN ItemDetail id ON ri.requestItemID = id.requestItemID WHERE ri.jobRequestID = @JobRequestID";
                using (SqlCommand cmdDetails = new SqlCommand(checkDetailsQuery, conn))
                {
                    cmdDetails.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    if ((int)cmdDetails.ExecuteScalar() == 0)
                    {
                        ShowMessage("Cannot generate a quote yet. Site measurements or item details have not been provided for this job request.", "alert alert-danger rounded-3 mb-4", false);
                        return;
                    }
                }

                // 2. Check if a quote already exists
                string checkQuoteQuery = "SELECT COUNT(*) FROM Quote WHERE jobRequestID = @JobRequestID";
                using (SqlCommand cmdQuoteCheck = new SqlCommand(checkQuoteQuery, conn))
                {
                    cmdQuoteCheck.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    if ((int)cmdQuoteCheck.ExecuteScalar() > 0)
                    {
                        ShowMessage("A quote for this job request already exists. A new quote cannot be created.", "alert alert-warning rounded-3 mb-4", true);
                        return;
                    }
                }

                // 3. Calculate total amount
                decimal servicesSubtotal = GetServicesSubtotal(jobRequestId);

                double lat = string.IsNullOrEmpty(hfLatitude.Value) ? 0.0 : Convert.ToDouble(hfLatitude.Value);
                double lon = string.IsNullOrEmpty(hfLongitude.Value) ? 0.0 : Convert.ToDouble(hfLongitude.Value);

                decimal travelFee = CalculateTravelFee(lat, lon);
                decimal subtotalWithTravel = servicesSubtotal + travelFee;
                decimal vat = subtotalWithTravel * 0.15m;
                decimal totalWithVat = subtotalWithTravel + vat;

                // 4. Insert into Quote
                string insertQuoteQuery = @"INSERT INTO Quote (jobRequestID, dateIssued, expiryDate, dateGenerated, amount, quoteStatus) 
                                            VALUES (@JobRequestID, GETDATE(), DATEADD(month, 1, GETDATE()), GETDATE(), @Amount, 'Sent')";

                using (SqlCommand cmdInsert = new SqlCommand(insertQuoteQuery, conn))
                {
                    cmdInsert.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    cmdInsert.Parameters.AddWithValue("@Amount", totalWithVat);
                    cmdInsert.ExecuteNonQuery();
                }

                // 5. Update JobRequest status
                string updateJobRequestQuery = "UPDATE JobRequest SET status = 'Quote Sent' WHERE jobRequestID = @JobRequestID";
                using (SqlCommand cmdJobUpdate = new SqlCommand(updateJobRequestQuery, conn))
                {
                    cmdJobUpdate.Parameters.AddWithValue("@JobRequestID", jobRequestId);
                    cmdJobUpdate.ExecuteNonQuery();
                }
            }

            ShowMessage("Quote successfully generated, saved, and marked as Sent! You can now view your quote document below.", "alert alert-success rounded-3 mb-4", true);
        }

        private void ShowMessage(string message, string cssClass, bool showPdfButton)
        {
            lblMessage.Text = message;
            pnlMessage.CssClass = cssClass;
            pnlMessage.Visible = true;
            btnViewPdfQuote.Visible = showPdfButton;
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            gvJobRequests.SelectedIndex = -1;
            txtSelectedJobId.Text = string.Empty;
            txtSiteAddress.Text = string.Empty;
            hfLongitude.Value = string.Empty;
            hfLatitude.Value = string.Empty;

            gvRequestItems.DataSource = null;
            gvRequestItems.DataBind();

            lblSubtotal.Text = "R 0,00";
            lblTravelFee.Text = "R 0,00";
            lblSubtotalWithTravel.Text = "R 0,00";
            lblVat.Text = "R 0,00";
            lblTotalWithVat.Text = "R 0,00";
            lblOneWayDistance.Text = "0.0 km";
            lblDisplaySiteAddress.Text = "[No Address Selected]";

            btnViewPdfQuote.Visible = false;
            pnlMessage.Visible = false;

            // Reset visibility states back to initial landing state
            pnlFormContent.Visible = false;
            pnlSelectionDetails.Visible = false;
            pnlSelectionPrompt.Visible = true;
        }

        protected void btnViewPdfQuote_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSelectedJobId.Text))
            {
                Response.Redirect($"PrintQuote.aspx?jobRequestID={txtSelectedJobId.Text}");
            }
        }
    }
}