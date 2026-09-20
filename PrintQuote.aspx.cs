using System;
using System.Data.SqlClient;
using System.Configuration;

namespace GiantGroupM3
{
    public partial class PrintQuote : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string jobId = Request.QueryString["jobRequestID"];

                if (!string.IsNullOrEmpty(jobId))
                {
                    LoadQuoteFromDatabase(jobId);
                }
                else
                {
                    LoadQuoteFromSession();
                }
            }
        }

        private void LoadQuoteFromDatabase(string jobId)
        {
            string connString = ConfigurationManager.ConnectionStrings["GiantGroupDB"]?.ConnectionString
                         ?? ConfigurationManager.ConnectionStrings["defaultConnection"]?.ConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                LoadQuoteFromSession();
                return;
            }

            using (SqlConnection conn = new SqlConnection(connString))
            {
                string query = @"SELECT jr.jobRequestID, jr.siteAddress, jr.dateRecieved, 
                                q.QuoteID, q.dateIssued, q.amount 
                         FROM jobRequest jr 
                         LEFT JOIN Quote q ON jr.jobRequestID = q.jobRequestID 
                         WHERE jr.jobRequestID = @JobId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@JobId", jobId);
                    conn.Open();

                    string quoteId = "";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            quoteId = reader["QuoteID"]?.ToString();

                            DateTime quoteDate = reader["dateIssued"] != DBNull.Value
                                ? Convert.ToDateTime(reader["dateIssued"])
                                : (reader["dateRecieved"] != DBNull.Value ? Convert.ToDateTime(reader["dateRecieved"]) : DateTime.Now);

                            lblQuoteDate.Text = quoteDate.ToString("dd MMMM yyyy");
                            lblQuoteNo.Text = $"Quotation NO. GT – {jobId}";

                            lblRecipientName.Text = "Valued Client";
                            lblRecipientAddress.Text = reader["siteAddress"]?.ToString() ?? "Durban, KwaZulu-Natal";

                            decimal total = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0.00m;
                            decimal subtotal = total / 1.15m;
                            decimal vat = total - subtotal;
                            decimal servicesCost = subtotal > 150.00m ? subtotal - 150.00m : subtotal;
                            decimal travelFee = subtotal > 150.00m ? 150.00m : 0.00m;

                            lblServiceDescription.Text = $"Professional Tree Felling & Site Clearance (Job #{jobId})";
                            lblServicesSubtotal.Text = $"R {servicesCost:N2}";
                            lblTravelFee.Text = $"R {travelFee:N2}";
                            lblSubtotalInclTravel.Text = $"R {subtotal:N2}";
                            lblVat.Text = $"R {vat:N2}";
                            lblTotalAmount.Text = $"R {total:N2}";
                        }
                        else
                        {
                            LoadQuoteFromSession();
                            return;
                        }
                    }

                    // Automatically record/update the filePath column in the Quote table
                    if (!string.IsNullOrEmpty(quoteId))
                    {
                        string updatePathQuery = "UPDATE Quote SET filePath = @FilePath WHERE QuoteID = @QuoteID";
                        using (SqlCommand updateCmd = new SqlCommand(updatePathQuery, conn))
                        {
                            string targetPath = $"~/PrintQuote.aspx?jobRequestID={jobId}";
                            updateCmd.Parameters.AddWithValue("@FilePath", targetPath);
                            updateCmd.Parameters.AddWithValue("@QuoteID", quoteId);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void LoadQuoteFromSession()
        {
            lblQuoteDate.Text = DateTime.Now.ToString("dd MMMM yyyy");
            lblQuoteNo.Text = "Quotation NO. GT – 39";

            if (Session["Quote_Total"] != null)
            {
                lblServicesSubtotal.Text = Session["Quote_ServicesSubtotal"]?.ToString() ?? "R 0,00";
                lblTravelFee.Text = Session["Quote_TravelFee"]?.ToString() ?? "R 0,00";
                lblSubtotalInclTravel.Text = Session["Quote_SubtotalInclTravel"]?.ToString() ?? "R 0,00";
                lblVat.Text = Session["Quote_Vat"]?.ToString() ?? "R 0,00";
                lblTotalAmount.Text = Session["Quote_Total"]?.ToString() ?? "R 0,00";
            }
        }
    }
}