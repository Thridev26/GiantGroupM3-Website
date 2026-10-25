using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class EvaluateRequest : Page
    {
        private readonly string _connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
        private int _requestId;

        protected void Page_Init(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!IsOperationsAuthorized())
            {
                Response.Redirect("~/Account/Login.aspx", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!int.TryParse(Request.QueryString["id"], out _requestId) || _requestId <= 0)
                return;

            try
            {
                if (RequestHasEvaluationDate(_requestId))
                    BuildDetailFields(_requestId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("EvaluateRequest control setup failed: " + ex);
                ShowMessage("The request details could not be loaded. Please return to Manage Requests and try again.");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsOperationsAuthorized()) return;

            if (!int.TryParse(Request.QueryString["id"], out _requestId) || _requestId <= 0)
            {
                ShowMessage("A valid request number was not supplied.");
                return;
            }

            try
            {
                LoadRequestHeader(_requestId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("EvaluateRequest header load failed: " + ex);
                ShowMessage("The request could not be loaded. Please return to Manage Requests and try again.");
            }
        }

        private bool IsOperationsAuthorized()
        {
            if (!Request.IsAuthenticated) return false;
            string role = Convert.ToString(Session["UserRole"] ?? Session["UserType"] ?? "");
            int level = 0;
            int.TryParse(Convert.ToString(Session["UserLevel"] ?? Session["AccessLevel"]), out level);
            return level >= 4 || role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Owner", StringComparison.OrdinalIgnoreCase);
        }

        private bool RequestHasEvaluationDate(int requestId)
        {
            if (string.IsNullOrWhiteSpace(_connString)) return false;
            using (SqlConnection conn = new SqlConnection(_connString))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.JobRequest WHERE jobRequestID = @id AND siteEvaluationDate IS NOT NULL", conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = requestId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void LoadRequestHeader(int requestId)
        {
            const string sql = @"
                SELECT jr.jobRequestID, jr.siteAddress, jr.urgencyLevel, jr.status,
                       jr.siteEvaluationDate, c.clientName, c.clientSurname
                FROM dbo.JobRequest jr
                INNER JOIN dbo.Client c ON c.clientID = jr.clientID
                WHERE jr.jobRequestID = @id AND jr.siteEvaluationDate IS NOT NULL;";

            using (SqlConnection conn = new SqlConnection(_connString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = requestId;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        pnlRequest.Visible = false;
                        ShowMessage("This request does not have a site evaluation date. The Systems Manager must set the date before it can be evaluated.");
                        return;
                    }

                    pnlRequest.Visible = true;
                    litRequestID.Text = HttpUtility.HtmlEncode(Convert.ToString(reader["jobRequestID"]));
                    litClient.Text = HttpUtility.HtmlEncode(Convert.ToString(reader["clientName"]) + " " + Convert.ToString(reader["clientSurname"]));
                    litAddress.Text = HttpUtility.HtmlEncode(Convert.ToString(reader["siteAddress"]));
                    litUrgency.Text = HttpUtility.HtmlEncode(Convert.ToString(reader["urgencyLevel"]));
                    litEvaluationDate.Text = Convert.ToDateTime(reader["siteEvaluationDate"]).ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("en-ZA"));
                    string status = Convert.ToString(reader["status"]);
                    litStatus.Text = HttpUtility.HtmlEncode(status);
                    bool alreadyEvaluated = status.Equals("Site Evaluated", StringComparison.OrdinalIgnoreCase);
                    btnSiteEvaluated.Enabled = !alreadyEvaluated;
                    btnSiteEvaluated.Text = alreadyEvaluated ? "Site Evaluated ✓" : "Site Evaluated";
                }
            }
        }

        private void BuildDetailFields(int requestId)
        {
            const string sql = @"
                SELECT ri.requestItemID, ri.jobTypeID, jt.jobTypeName,
                       jd.jobDetailID, jd.detailName, jd.dataType, id.detailValue
                FROM dbo.RequestItem ri
                INNER JOIN dbo.JobType jt ON jt.jobTypeID = ri.jobTypeID
                LEFT JOIN dbo.JobDetail jd ON jd.jobTypeID = ri.jobTypeID
                LEFT JOIN dbo.ItemDetail id ON id.jobDetailID = jd.jobDetailID AND id.requestItemID = ri.requestItemID
                WHERE ri.jobRequestID = @id
                ORDER BY ri.requestItemID, jd.jobDetailID;";

            DataTable rows = new DataTable();
            using (SqlConnection conn = new SqlConnection(_connString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = requestId;
                adapter.Fill(rows);
            }

            phDetails.Controls.Clear();
            if (rows.Rows.Count == 0)
            {
                phDetails.Controls.Add(new LiteralControl("<p class=\"text-muted\">No requested items were found for this request.</p>"));
                return;
            }

            int? currentItemId = null;
            Panel itemPanel = null;
            bool itemHasFields = false;

            foreach (DataRow row in rows.Rows)
            {
                int itemId = Convert.ToInt32(row["requestItemID"]);
                if (currentItemId != itemId)
                {
                    currentItemId = itemId;
                    itemHasFields = false;
                    itemPanel = new Panel { CssClass = "ev-item" };
                    Label title = new Label { CssClass = "ev-item-title", Text = HttpUtility.HtmlEncode(Convert.ToString(row["jobTypeName"])) };
                    itemPanel.Controls.Add(title);
                    phDetails.Controls.Add(itemPanel);
                }

                if (row.IsNull("jobDetailID"))
                {
                    itemPanel.Controls.Add(new LiteralControl("<p class=\"text-muted\">No job detail fields have been configured for this item.</p>"));
                    continue;
                }

                itemHasFields = true;
                int jobDetailId = Convert.ToInt32(row["jobDetailID"]);
                string detailName = Convert.ToString(row["detailName"]);
                string dataType = Convert.ToString(row["dataType"]);
                string existingValue = row.IsNull("detailValue") ? string.Empty : Convert.ToString(row["detailValue"]);

                Panel field = new Panel { CssClass = "ev-field" };
                Label label = new Label { Text = HttpUtility.HtmlEncode(detailName), AssociatedControlID = "txtDetail_" + jobDetailId + "_" + itemId };
                TextBox input = new TextBox
                {
                    ID = "txtDetail_" + jobDetailId + "_" + itemId,
                    CssClass = "form-control",
                    MaxLength = 50,
                    Text = IsPostBack ? string.Empty : existingValue
                };
                input.Attributes["autocomplete"] = "off";
                Label hint = new Label { CssClass = "ev-hint", Text = HttpUtility.HtmlEncode(GetDataTypeHint(dataType)) };
                field.Controls.Add(label);
                field.Controls.Add(input);
                field.Controls.Add(hint);
                itemPanel.Controls.Add(field);
            }
        }

        protected void btnCapture_Click(object sender, EventArgs e)
        {
            if (_requestId <= 0 || !RequestHasEvaluationDate(_requestId))
            {
                ShowMessage("This request cannot be evaluated until the Systems Manager has set its site evaluation date.");
                return;
            }

            List<DetailToSave> values = new List<DetailToSave>();
            foreach (TextBox input in FindTextBoxes(phDetails))
            {
                if (string.IsNullOrWhiteSpace(input.Text) || input.Text.Trim().Equals("Not Specified", StringComparison.OrdinalIgnoreCase))
                    continue;

                string[] parts = input.ID.Split('_');
                if (parts.Length != 3 || !int.TryParse(parts[1], out int jobDetailId) || !int.TryParse(parts[2], out int requestItemId))
                {
                    ShowMessage("A detail field could not be identified. Refresh the page and try again.");
                    return;
                }

                string value = input.Text.Trim();
                if (value.Length > 50)
                {
                    input.Focus();
                    ShowMessage("Each detail must be 50 characters or fewer. Please shorten the highlighted value.");
                    return;
                }

                string dataType = GetValidatedDataType(jobDetailId, requestItemId, _requestId);
                if (dataType == null)
                {
                    ShowMessage("A detail field did not match this request. Refresh the page and try again.");
                    return;
                }

                string validationError = ValidateDetailValue(value, dataType);
                if (validationError != null)
                {
                    input.Focus();
                    ShowMessage("'" + value + "' " + validationError + " for " + GetDetailName(jobDetailId) + ".");
                    return;
                }

                values.Add(new DetailToSave { JobDetailID = jobDetailId, RequestItemID = requestItemId, Value = value });
            }

            if (values.Count == 0)
            {
                ShowMessage("Enter at least one measurement or detail before clicking Capture details.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            foreach (DetailToSave item in values)
                            {
                                int? existingId = null;
                                using (SqlCommand check = new SqlCommand(@"SELECT TOP 1 itemDetailID FROM dbo.ItemDetail WITH (UPDLOCK, HOLDLOCK) WHERE jobDetailID = @detailID AND requestItemID = @itemID", conn, tx))
                                {
                                    check.Parameters.Add("@detailID", SqlDbType.Int).Value = item.JobDetailID;
                                    check.Parameters.Add("@itemID", SqlDbType.Int).Value = item.RequestItemID;
                                    object found = check.ExecuteScalar();
                                    if (found != null && found != DBNull.Value) existingId = Convert.ToInt32(found);
                                }

                                string saveSql = existingId.HasValue
                                    ? "UPDATE dbo.ItemDetail SET detailValue = @value WHERE itemDetailID = @id"
                                    : "INSERT INTO dbo.ItemDetail (detailValue, jobDetailID, requestItemID) VALUES (@value, @detailID, @itemID)";
                                using (SqlCommand save = new SqlCommand(saveSql, conn, tx))
                                {
                                    save.Parameters.Add("@value", SqlDbType.VarChar, 50).Value = item.Value;
                                    if (existingId.HasValue)
                                        save.Parameters.Add("@id", SqlDbType.Int).Value = existingId.Value;
                                    else
                                    {
                                        save.Parameters.Add("@detailID", SqlDbType.Int).Value = item.JobDetailID;
                                        save.Parameters.Add("@itemID", SqlDbType.Int).Value = item.RequestItemID;
                                    }
                                    save.ExecuteNonQuery();
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

                ShowMessage(values.Count + " detail value(s) captured successfully. You can update them later by reopening this request.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("EvaluateRequest capture failed: " + ex);
                ShowMessage("The details could not be saved. Please check the database connection and try again.");
            }
        }

        protected void btnSiteEvaluated_Click(object sender, EventArgs e)
        {
            if (_requestId <= 0) return;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                using (SqlCommand cmd = new SqlCommand(@"UPDATE dbo.JobRequest SET status = 'Site Evaluated' WHERE jobRequestID = @id AND siteEvaluationDate IS NOT NULL", conn))
                {
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = _requestId;
                    conn.Open();
                    int changed = cmd.ExecuteNonQuery();
                    if (changed == 1)
                    {
                        litStatus.Text = "Site Evaluated";
                        btnSiteEvaluated.Text = "Site Evaluated ✓";
                        btnSiteEvaluated.Enabled = false;
                        ShowMessage("The request status is now Site Evaluated.");
                    }
                    else
                    {
                        ShowMessage("The status was not changed because this request does not have a site evaluation date.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("EvaluateRequest status update failed: " + ex);
                ShowMessage("The request status could not be updated. Please try again.");
            }
        }

        private string GetValidatedDataType(int jobDetailId, int requestItemId, int requestId)
        {
            const string sql = @"
                SELECT jd.dataType
                FROM dbo.JobDetail jd
                INNER JOIN dbo.RequestItem ri ON ri.jobTypeID = jd.jobTypeID
                WHERE jd.jobDetailID = @detailID AND ri.requestItemID = @itemID AND ri.jobRequestID = @requestID;";
            using (SqlConnection conn = new SqlConnection(_connString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@detailID", SqlDbType.Int).Value = jobDetailId;
                cmd.Parameters.Add("@itemID", SqlDbType.Int).Value = requestItemId;
                cmd.Parameters.Add("@requestID", SqlDbType.Int).Value = requestId;
                conn.Open();
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : Convert.ToString(result);
            }
        }

        private string GetDetailName(int jobDetailId)
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            using (SqlCommand cmd = new SqlCommand("SELECT detailName FROM dbo.JobDetail WHERE jobDetailID = @id", conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = jobDetailId;
                conn.Open();
                object result = cmd.ExecuteScalar();
                return result == null ? "the selected field" : Convert.ToString(result);
            }
        }

        private static IEnumerable<TextBox> FindTextBoxes(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is TextBox textBox) yield return textBox;
                foreach (TextBox nested in FindTextBoxes(child)) yield return nested;
            }
        }

        private static string GetDataTypeHint(string dataType)
        {
            switch ((dataType ?? string.Empty).ToLowerInvariant())
            {
                case "int": return "Whole number (e.g. 5)";
                case "decimal": return "Number (e.g. 12.50)";
                case "date": return "Date (dd/MM/yyyy)";
                case "boolean": return "Yes or No";
                case "varchar": return "Text (maximum 50 characters)";
                default: return "Enter a value";
            }
        }

        private static string ValidateDetailValue(string value, string dataType)
        {
            switch ((dataType ?? string.Empty).ToLowerInvariant())
            {
                case "int":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return "must be a whole number (e.g. 5)";
                    break;
                case "decimal":
                    if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                        && !decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("en-ZA"), out _)) return "must be a number (e.g. 12.50)";
                    break;
                case "date":
                    if (!DateTime.TryParseExact(value, new[] { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return "must be a valid date (dd/MM/yyyy)";
                    break;
                case "boolean":
                    string v = value.Trim().ToLowerInvariant();
                    if (v != "yes" && v != "no" && v != "true" && v != "false") return "must be Yes or No";
                    break;
                case "varchar":
                    break;
                default:
                    return "has an unsupported field type";
            }
            return null;
        }

        private void ShowMessage(string message)
        {
            lblMessage.Text = HttpUtility.HtmlEncode(message);
            lblMessage.Visible = true;
        }

        private sealed class DetailToSave
        {
            public int JobDetailID { get; set; }
            public int RequestItemID { get; set; }
            public string Value { get; set; }
        }
    }
}
