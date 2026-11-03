using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3.Requests
{
    public partial class ViewRequests : Page
    {
        private string ConnectionString { get { return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString; } }
        private int ClientId { get { int v; return int.TryParse(Convert.ToString(Session["ClientID"]), out v) ? v : 0; } }
        private bool Authorized { get { return Request.IsAuthenticated && ClientId > 0 && string.Equals(Convert.ToString(Session["UserRole"]), "Client", StringComparison.OrdinalIgnoreCase); } }
        private static DateTime SouthAfricaToday()
        {
            try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time")).Date; }
            catch (TimeZoneNotFoundException) { return DateTime.UtcNow.AddHours(2).Date; }
            catch (InvalidTimeZoneException) { return DateTime.UtcNow.AddHours(2).Date; }
        }
        private static bool CanCancel(string status, object date)
        {
            return (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "Evaluation date set", StringComparison.OrdinalIgnoreCase)) &&
                   (date == null || date == DBNull.Value || Convert.ToDateTime(date).Date > SouthAfricaToday());
        }
      
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (!Authorized)
            {
                Response.Redirect(ResolveUrl("~/Account/Login.aspx"), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            int photoId;
            if (Request.HttpMethod == "GET" && int.TryParse(Request.QueryString["photo"], out photoId) && photoId > 0)
            {
                ServePhoto(photoId);
                return;
            }
            if (!IsPostBack) BindRequests();
        }
        private void BindRequests()
        {
            const string sql = @"SELECT jr.jobRequestID,jr.dateRecieved,jr.siteAddress,jr.urgencyLevel,
                COALESCE(jr.status,'Pending') AS RequestStatus,jr.siteEvaluationDate,
                STUFF((SELECT ', ' + jt.jobTypeName FROM dbo.RequestItem ri
                INNER JOIN dbo.JobType jt ON jt.jobTypeID=ri.jobTypeID
                WHERE ri.jobRequestID=jr.jobRequestID ORDER BY jt.jobTypeName
                FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,2,'') AS JobTypes
                FROM dbo.JobRequest jr WHERE jr.clientID=@ClientID
                ORDER BY jr.dateRecieved DESC,jr.jobRequestID DESC;";
            try
            {
                using (var cn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, cn))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                    var dt = new DataTable(); da.Fill(dt); gvRequests.DataSource = dt; gvRequests.DataBind();
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Request listing: " + ex); pnlError.Visible = true; }
        }
        protected void gvRequests_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;
            var b = e.Row.FindControl("btnCancel") as LinkButton;
            if (b != null)
            {
                var r = (DataRowView)e.Row.DataItem;
                b.Visible = CanCancel(Convert.ToString(r["RequestStatus"]), r["siteEvaluationDate"]);
            }
        }
        protected void gvRequests_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id; if (!int.TryParse(Convert.ToString(e.CommandArgument), out id) || id <= 0) return;
            if (e.CommandName == "CancelRequest") CancelRequest(id);
            if (e.CommandName == "Photos") OpenPhotos(id);
        }
        private void ShowMessage(string message) { pnlMessage.Visible = true; litMessage.Text = message; }
        private void CancelRequest(int requestId)
        {
            const string sql = @"UPDATE dbo.JobRequest SET status='Cancelled'
               WHERE jobRequestID=@RequestID AND clientID=@ClientID
               AND status IN ('Pending','Evaluation date set')
               AND (siteEvaluationDate IS NULL OR siteEvaluationDate>@Today);
               IF @@ROWCOUNT=1
               BEGIN
                  INSERT INTO dbo.RequestCancellationNotification(JobRequestID,ClientID)
                  VALUES(@RequestID,@ClientID);
                  SELECT 1;
               END
               ELSE SELECT 0;";
            try
            {
                using (var cn = new SqlConnection(ConnectionString))
                {
                    cn.Open(); using (var tx = cn.BeginTransaction())
                    {
                        using (var cmd = new SqlCommand(sql, cn, tx))
                        {
                            cmd.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                            cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                            cmd.Parameters.Add("@Today", SqlDbType.Date).Value = SouthAfricaToday();
                            int updated = Convert.ToInt32(cmd.ExecuteScalar()); tx.Commit();
                            ShowMessage(updated == 1 ? "Request cancelled. The management team has been notified." : "This request cannot be cancelled (its status or evaluation date may have changed).");
                        }
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Cancel request: " + ex); ShowMessage("Cancellation failed. Please try again or call 084 833 1373."); }
            pnlPhotos.Visible = false; BindRequests();
        }
        private bool GetOwnedRequest(int id, out string status)
        {
            status = null;
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand("SELECT status FROM dbo.JobRequest WHERE jobRequestID=@ID AND clientID=@ClientID", cn))
            {
                cmd.Parameters.Add("@ID", SqlDbType.Int).Value = id;
                cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                cn.Open(); var result = cmd.ExecuteScalar();
                if (result == null) return false;
                status = Convert.ToString(result); return true;
            }
        }
        private void OpenPhotos(int id)
        {
            try
            {
                string status; if (!GetOwnedRequest(id, out status)) { ShowMessage("Request not found."); return; }
                hfRequestId.Value = id.ToString(CultureInfo.InvariantCulture);
                litPhotoTitle.Text = "Site photos for request #" + id;
                pnlPhotos.Visible = true;
                bool cancelled = string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase);
                pnlUpload.Visible = !cancelled; pnlReadOnly.Visible = cancelled;
                BindPhotos(id, cancelled);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Open photos: " + ex); ShowMessage("Could not open photos."); }
        }
        private void BindPhotos(int requestId, bool cancelled)
        {
            const string sql = @"SELECT sp.photoID,sp.uploadDate,CAST(CASE WHEN @Cancelled=1 THEN 0 ELSE 1 END AS bit) AS CanDelete
              FROM dbo.SitePhoto sp INNER JOIN dbo.SitePhotoContent sc ON sc.photoID=sp.photoID
              INNER JOIN dbo.JobRequest jr ON jr.jobRequestID=sp.jobRequestID
              WHERE sp.jobRequestID=@ID AND jr.clientID=@ClientID AND sp.jobID IS NULL
              AND sc.UploadedByClient=1 ORDER BY sp.photoID DESC";
            using (var cn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(sql, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@ID", SqlDbType.Int).Value = requestId;
                cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                cmd.Parameters.Add("@Cancelled", SqlDbType.Bit).Value = cancelled;
                var dt = new DataTable(); da.Fill(dt); rptPhotos.DataSource = dt; rptPhotos.DataBind(); lblNoPhotos.Visible = dt.Rows.Count == 0;
            }
        }
        protected string PhotoUrl(object id) { return ResolveUrl("~/Requests/ViewRequests.aspx?photo=" + Convert.ToString(id, CultureInfo.InvariantCulture)); }
        private void ServePhoto(int photoId)
        {
            const string sql = @"SELECT sc.MimeType,sc.ImageData FROM dbo.SitePhoto sp
               INNER JOIN dbo.SitePhotoContent sc ON sc.photoID=sp.photoID
               INNER JOIN dbo.JobRequest jr ON jr.jobRequestID=sp.jobRequestID
               WHERE sp.photoID=@PhotoID AND jr.clientID=@ClientID AND sp.jobID IS NULL AND sc.UploadedByClient=1;";
            try
            {
                using (var cn = new SqlConnection(ConnectionString))
                using (var cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.Add("@PhotoID", SqlDbType.Int).Value = photoId;
                    cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                    cn.Open(); using (var rdr = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        if (!rdr.Read()) { Response.StatusCode = 404; Context.ApplicationInstance.CompleteRequest(); return; }
                        string mime = rdr.GetString(0); byte[] data = (byte[])rdr["ImageData"];
                        Response.Clear(); Response.ContentType = mime; Response.AddHeader("X-Content-Type-Options", "nosniff");
                        Response.BinaryWrite(data); Response.Flush(); Context.ApplicationInstance.CompleteRequest();
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Serving photo: " + ex); Response.StatusCode = 500; Context.ApplicationInstance.CompleteRequest(); }
        }
        protected void btnUpload_Click(object sender, EventArgs e)
        {
            int id; if (!Authorized || !int.TryParse(hfRequestId.Value, out id) || id <= 0) return;
            try
            {
                string status; if (!GetOwnedRequest(id, out status) || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)) { ShowMessage("Photos cannot be added to this request."); return; }
                if (!fuPhoto.HasFile || fuPhoto.PostedFile.ContentLength == 0 || fuPhoto.PostedFile.ContentLength > 5 * 1024 * 1024) { ShowMessage("Choose a JPG or PNG photo smaller than 5 MB."); OpenPhotos(id); return; }
                byte[] bytes;
                using (var ms = new MemoryStream()) { fuPhoto.PostedFile.InputStream.CopyTo(ms); bytes = ms.ToArray(); }
                string mime = null;
                if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) mime = "image/jpeg";
                if (bytes.Length >= 8 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 && bytes[4] == 13 && bytes[5] == 10 && bytes[6] == 26 && bytes[7] == 10) mime = "image/png";
                if (mime == null) { ShowMessage("Only valid JPG and PNG files are allowed."); OpenPhotos(id); return; }
                using (var cn = new SqlConnection(ConnectionString))
                {
                    cn.Open(); using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        using (var lockCmd = new SqlCommand("SELECT status FROM dbo.JobRequest WITH (UPDLOCK,HOLDLOCK) WHERE jobRequestID=@ID AND clientID=@ClientID", cn, tx))
                        {
                            lockCmd.Parameters.Add("@ID", SqlDbType.Int).Value = id; lockCmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                            var locked = lockCmd.ExecuteScalar(); if (locked == null || string.Equals(Convert.ToString(locked), "Cancelled", StringComparison.OrdinalIgnoreCase)) { tx.Rollback(); ShowMessage("This request is no longer available for uploads."); return; }
                        }
                        int photoId;
                        using (var cmd = new SqlCommand(@"INSERT INTO dbo.SitePhoto(photoType,filePath,uploadDate,jobRequestID)
                      VALUES('BEFORE',@Path,GETDATE(),@ID); SELECT CAST(SCOPE_IDENTITY() AS int);", cn, tx))
                        { cmd.Parameters.Add("@Path", SqlDbType.VarChar, 255).Value = "db-photo://client/" + Guid.NewGuid().ToString("N"); cmd.Parameters.Add("@ID", SqlDbType.Int).Value = id; photoId = Convert.ToInt32(cmd.ExecuteScalar()); }
                        using (var cmd = new SqlCommand("INSERT INTO dbo.SitePhotoContent(photoID,MimeType,ImageData,UploadedByClient) VALUES(@ID,@Mime,@Data,1)", cn, tx))
                        { cmd.Parameters.Add("@ID", SqlDbType.Int).Value = photoId; cmd.Parameters.Add("@Mime", SqlDbType.VarChar, 20).Value = mime; cmd.Parameters.Add("@Data", SqlDbType.VarBinary, -1).Value = bytes; cmd.ExecuteNonQuery(); }
                        tx.Commit();
                    }
                }
                ShowMessage("Photo uploaded successfully."); OpenPhotos(id);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Upload site photo: " + ex); ShowMessage("Photo upload failed. Please try again."); OpenPhotos(id); }
        }
        protected void rptPhotos_ItemCommand(object sender, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "RemovePhoto" || !Authorized) return;
            int id, photoId; if (!int.TryParse(hfRequestId.Value, out id) || !int.TryParse(Convert.ToString(e.CommandArgument), out photoId)) return;
            try
            {
                using (var cn = new SqlConnection(ConnectionString))
                {
                    cn.Open(); using (var tx = cn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        using (var lockCmd = new SqlCommand("SELECT status FROM dbo.JobRequest WITH (UPDLOCK,HOLDLOCK) WHERE jobRequestID=@ID AND clientID=@ClientID", cn, tx))
                        {
                            lockCmd.Parameters.Add("@ID", SqlDbType.Int).Value = id; lockCmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = ClientId;
                            var status = lockCmd.ExecuteScalar(); if (status == null || string.Equals(Convert.ToString(status), "Cancelled", StringComparison.OrdinalIgnoreCase)) { tx.Rollback(); ShowMessage("Photos on cancelled requests cannot be removed."); return; }
                        }
                        using (var cmd = new SqlCommand(@"DELETE sc FROM dbo.SitePhotoContent sc
                     JOIN dbo.SitePhoto sp ON sp.photoID=sc.photoID
                     WHERE sp.photoID=@PhotoID AND sp.jobRequestID=@ID AND sp.jobID IS NULL AND sc.UploadedByClient=1;
                     IF @@ROWCOUNT=1 DELETE FROM dbo.SitePhoto WHERE photoID=@PhotoID AND jobRequestID=@ID AND jobID IS NULL;", cn, tx))
                        { cmd.Parameters.Add("@PhotoID", SqlDbType.Int).Value = photoId; cmd.Parameters.Add("@ID", SqlDbType.Int).Value = id; cmd.ExecuteNonQuery(); }
                        tx.Commit();
                    }
                }
                ShowMessage("Photo removed."); OpenPhotos(id);
            }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("Remove photo: " + ex); ShowMessage("Could not remove photo."); OpenPhotos(id); }
        }
        protected void btnClosePhotos_Click(object sender, EventArgs e) { pnlPhotos.Visible = false; hfRequestId.Value = string.Empty; }
    }
}
