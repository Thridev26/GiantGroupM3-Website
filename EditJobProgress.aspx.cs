
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class EditJobProgress : Page
    {
        // Keeps uploads below the usual ASP.NET
        // default 4 MB request limit.
        private const int MaxPhotoBytes = 3 * 1024 * 1024;

        private string ConnectionString
        {
            get
            {
                ConnectionStringSettings setting =
                    ConfigurationManager.ConnectionStrings[
                        "DefaultConnection"];

                if (setting == null ||
                    string.IsNullOrWhiteSpace(
                        setting.ConnectionString))
                {
                    throw new ConfigurationErrorsException(
                        "DefaultConnection is missing.");
                }

                return setting.ConnectionString;
            }
        }

        private int SelectedJobId
        {
            get
            {
                object value = ViewState["SelectedJobId"];

                return value == null
                    ? 0
                    : Convert.ToInt32(value);
            }

            set
            {
                ViewState["SelectedJobId"] = value;
            }
        }

        private string LoadedStatus
        {
            get
            {
                return Convert.ToString(
                    ViewState["LoadedStatus"]);
            }

            set
            {
                ViewState["LoadedStatus"] = value;
            }
        }

        // ============================================
        // PAGE LOAD
        // ============================================


        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(
                HttpCacheability.NoCache);

            Response.Cache.SetNoStore();

            // Check whether the user is logged in.
            if (!Request.IsAuthenticated)
            {
                Response.Redirect(
                    ResolveUrl("~/Account/Login.aspx"),
                    true);
                return;
            }

            // Use the same permission rules
            // as the website's Site.Master.
            SiteMaster siteMaster = Master as SiteMaster;

            if (siteMaster == null)
            {
                throw new HttpException(
                    500, "Website master page is unavailable.");
            }

            int level = siteMaster.GetUserLevel();

            bool isStaff =
                Session["StaffID"] != null &&
                !siteMaster.IsClient();

            bool authorised =
                isStaff &&
                level >= 4 &&
                level <= 6 &&
                siteMaster.CanSeeEditJobProgress();

            if (!authorised)
            {
                throw new HttpException(
                    403,
                    "Access denied. Operations Manager, " +
                    "Admin or Owner access is required.");
            }

            if (!IsPostBack)
            {
                ClearJobDetails();

                try
                {
                    LoadJobsDropdown();

                    int requestedId;

                    if (int.TryParse(
                        Request.QueryString["jobId"],
                        out requestedId))
                    {
                        if (ddlJobs.Items.FindByValue(
                            requestedId.ToString()) != null)
                        {
                            LoadSelectedJob(requestedId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("Page_Load", ex);

                    ShowError(
                        "Unable to load jobs. " +
                        "Please check the database connection.");
                }
            }
        }

        // ============================================
        // MESSAGES
        // ============================================

        private void ShowMessage(
            string message,
            string cssClass)
        {
            lblMessage.Visible = true;
            lblMessage.CssClass = cssClass;

            lblMessage.Text =
                Server.HtmlEncode(message);
        }

        private void ShowSuccess(string message)
        {
            ShowMessage(
                message,
                "gg-msg is-success");
        }

        private void ShowWarning(string message)
        {
            ShowMessage(
                message,
                "gg-msg is-warning");
        }

        private void ShowError(string message)
        {
            ShowMessage(
                message,
                "gg-msg is-error");
        }

        private void LogError(
            string operation,
            Exception ex)
        {
            System.Diagnostics.Trace.TraceError(
                "EditJobProgress / " +
                operation + ": " +
                ex.ToString());
        }

        // ============================================
        // LOAD JOBS DROPDOWN
        // ============================================

        private void LoadJobsDropdown()
        {
            ddlJobs.Items.Clear();

            ddlJobs.Items.Add(
                new ListItem(
                    "-- Select an existing job --", ""));

            const string sql = @"
                SELECT
                    j.jobID,
                    j.jobStatus,
                    c.clientName,
                    c.clientSurname

                FROM dbo.Job j

                INNER JOIN dbo.Quote q
                    ON j.quoteID = q.QuoteID

                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID

                INNER JOIN dbo.Client c
                    ON jr.clientID = c.clientID

                ORDER BY
                    CASE
                        WHEN j.jobStatus IN
                            ('Completed', 'Archived')
                        THEN 1 ELSE 0
                    END,
                    j.jobID DESC;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(
                            reader["jobID"]);

                        string name =
                            Convert.ToString(
                                reader["clientName"]) +
                            " " +
                            Convert.ToString(
                                reader["clientSurname"]);

                        string status =
                            Convert.ToString(
                                reader["jobStatus"]);

                        string description =
                            "Job #" + id +
                            " - " + name +
                            " (" + status + ")";

                        ddlJobs.Items.Add(
                            new ListItem(
                                HttpUtility.HtmlEncode(description),
                                id.ToString()));
                    }
                }
            }
        }

        // ============================================
        // LOAD SELECTED JOB
        // ============================================

        protected void btnLoadJob_Click(
            object sender, EventArgs e)
        {
            int jobId;

            if (!int.TryParse(
                ddlJobs.SelectedValue,
                out jobId))
            {
                ClearJobDetails();

                ShowWarning(
                    "Please select a job first.");

                return;
            }

            try
            {
                LoadSelectedJob(jobId);
            }
            catch (Exception ex)
            {
                LogError("LoadSelectedJob", ex);

                ClearJobDetails();

                ShowError(
                    "Unable to load the selected job.");
            }
        }

        private void LoadSelectedJob(int jobId)
        {
            const string sql = @"
                SELECT
                    j.jobID,
                    j.quoteID,
                    j.jobStatus,
                    j.startDate,
                    c.clientName,
                    c.clientSurname,
                    jr.siteAddress

                FROM dbo.Job j

                INNER JOIN dbo.Quote q
                    ON j.quoteID = q.QuoteID

                INNER JOIN dbo.JobRequest jr
                    ON q.jobRequestID = jr.jobRequestID

                INNER JOIN dbo.Client c
                    ON jr.clientID = c.clientID

                WHERE j.jobID = @jobID;";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@jobID", SqlDbType.Int)
                        .Value = jobId;

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException(
                                "Job not found.");
                        }

                        SelectedJobId = jobId;

                        txtJobID.Text =
                            Convert.ToString(
                                reader["jobID"]);

                        txtQuoteID.Text =
                            Convert.ToString(
                                reader["quoteID"]);

                        txtClientName.Text =
                            Convert.ToString(
                                reader["clientName"]) +
                            " " +
                            Convert.ToString(
                                reader["clientSurname"]);

                        txtSiteAddress.Text =
                            Convert.ToString(
                                reader["siteAddress"]);

                        txtScheduledDate.Text =
                            reader["startDate"] == DBNull.Value
                                ? "Not scheduled"
                                : Convert.ToDateTime(
                                    reader["startDate"])
                                    .ToString("dd MMM yyyy");

                        LoadedStatus =
                            Convert.ToString(
                                reader["jobStatus"]);

                        txtCurrentStatus.Text =
                            LoadedStatus;
                    }
                }

                LoadJobTimeSlots(
                    connection, jobId);

                LoadCompletionPhotoInfo(
                    connection, jobId);
            }

            ListItem existing =
                ddlJobs.Items.FindByValue(
                    jobId.ToString());

            if (existing != null)
            {
                ddlJobs.SelectedValue =
                    jobId.ToString();
            }

            ConfigureStatusDropdown();
            UpdateProgressTracker();
        }

        // ============================================
        // LOAD JOB TIME SLOTS
        // ============================================

        private void LoadJobTimeSlots(
            SqlConnection connection,
            int jobId)
        {
            const string sql = @"
                SELECT
                    ts.startTime,
                    ts.endTime

                FROM dbo.JobTimeSlot jts

                INNER JOIN dbo.TimeSlot ts
                    ON jts.timeSlotID = ts.timeSlotID

                WHERE jts.jobID = @jobID

                ORDER BY ts.startTime;";

            List<string> slots =
                new List<string>();

            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@jobID", SqlDbType.Int)
                    .Value = jobId;

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        TimeSpan start =
                            (TimeSpan)reader["startTime"];

                        TimeSpan end =
                            (TimeSpan)reader["endTime"];

                        slots.Add(
                            start.ToString(@"hh\:mm") +
                            " - " +
                            end.ToString(@"hh\:mm"));
                    }
                }
            }

            txtTimeSlots.Text =
                slots.Count == 0
                    ? "No time slots assigned"
                    : string.Join(", ", slots);
        }

        // ============================================
        // COMPLETION PHOTO INFORMATION
        // ============================================

        private void LoadCompletionPhotoInfo(
            SqlConnection connection,
            int jobId)
        {
            const string sql = @"
                SELECT
                    COUNT(*) AS PhotoCount,
                    MAX(uploadDate) AS LatestDate

                FROM dbo.SitePhoto

                WHERE jobID = @jobID
                  AND photoType = 'AFTER';";

            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@jobID", SqlDbType.Int)
                    .Value = jobId;

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int count = Convert.ToInt32(
                            reader["PhotoCount"]);

                        if (count > 0)
                        {
                            lblPhotoInfo.Text =
                                count +
                                " completion photo(s) saved.";

                            txtCompletionDate.Text =
                                Convert.ToDateTime(
                                    reader["LatestDate"])
                                    .ToString("dd MMM yyyy");
                        }
                        else
                        {
                            lblPhotoInfo.Text =
                                "No completion photo saved.";

                            txtCompletionDate.Text = "";
                        }
                    }
                }
            }
        }

        // ============================================
        // STATUS RULES
        // ============================================

        private bool ValidTransition(
            string oldStatus,
            string newStatus)
        {
            if (oldStatus == "Not Started" &&
                newStatus == "In Progress")
            {
                return true;
            }

            if (oldStatus == "In Progress" &&
                newStatus == "Completed")
            {
                return true;
            }

            return false;
        }

        private void ConfigureStatusDropdown()
        {
            ddlNewStatus.Items.Clear();

            ddlNewStatus.Items.Add(
                new ListItem(
                    "-- Select next status --", ""));

            if (LoadedStatus == "Not Started")
            {
                ddlNewStatus.Items.Add(
                    new ListItem(
                        "In Progress", "In Progress"));
            }
            else if (LoadedStatus == "In Progress")
            {
                ddlNewStatus.Items.Add(
                    new ListItem(
                        "Completed", "Completed"));
            }

            ddlNewStatus.Enabled =
                SelectedJobId > 0 &&
                (LoadedStatus == "Not Started" ||
                 LoadedStatus == "In Progress");

            btnSaveProgress.Enabled = false;
        }

        protected void ddlNewStatus_SelectedIndexChanged(
            object sender, EventArgs e)
        {
            UpdateProgressTracker();
        }

        private void UpdateProgressTracker()
        {
            pnlNotStarted.CssClass = "ejp-stage";
            pnlInProgress.CssClass = "ejp-stage";
            pnlCompleted.CssClass = "ejp-stage";

            switch (LoadedStatus)
            {
                case "Not Started":
                    pnlNotStarted.CssClass =
                        "ejp-stage is-current";
                    break;

                case "In Progress":
                    pnlNotStarted.CssClass =
                        "ejp-stage is-done";

                    pnlInProgress.CssClass =
                        "ejp-stage is-current";
                    break;

                case "Completed":
                    pnlNotStarted.CssClass =
                        "ejp-stage is-done";

                    pnlInProgress.CssClass =
                        "ejp-stage is-done";

                    pnlCompleted.CssClass =
                        "ejp-stage is-current";
                    break;
            }

            string requested =
                ddlNewStatus.SelectedValue;

            pnlCompletion.Visible =
                SelectedJobId > 0 &&
                (requested == "Completed" ||
                 LoadedStatus == "Completed");

            btnSaveProgress.Enabled =
                SelectedJobId > 0 &&
                ValidTransition(
                    LoadedStatus, requested);
        }

        // ============================================
        // READ AND VALIDATE COMPLETION PHOTO
        // ============================================

        private class PhotoData
        {
            public byte[] Bytes;
            public string Extension;
        }

        private PhotoData ReadCompletionPhoto()
        {
            if (!fuCompletionPhoto.HasFile)
                return null;

            HttpPostedFile file =
                fuCompletionPhoto.PostedFile;

            if (file == null ||
                file.ContentLength <= 0 ||
                file.ContentLength > MaxPhotoBytes)
            {
                throw new InvalidOperationException(
                    "Photo must be 3 MB or smaller.");
            }

            byte[] bytes =
                fuCompletionPhoto.FileBytes;

            string extension = null;

            if (bytes.Length >= 3 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8 &&
                bytes[2] == 0xFF)
            {
                extension = ".jpg";
            }
            else if (bytes.Length >= 8 &&
                     bytes[0] == 0x89 &&
                     bytes[1] == 0x50 &&
                     bytes[2] == 0x4E &&
                     bytes[3] == 0x47 &&
                     bytes[4] == 0x0D &&
                     bytes[5] == 0x0A &&
                     bytes[6] == 0x1A &&
                     bytes[7] == 0x0A)
            {
                extension = ".png";
            }

            if (extension == null)
            {
                throw new InvalidOperationException(
                    "Only JPEG or PNG images are supported.");
            }

            return new PhotoData
            {
                Bytes = bytes,
                Extension = extension
            };
        }

        // ============================================
        // SAVE PHOTO TO SITEPHOTO TABLE
        // ============================================

        private string SaveCompletionPhoto(
            SqlConnection connection,
            SqlTransaction transaction,
            int jobId,
            int requestId,
            PhotoData photo)
        {
            string relativeFolder =
                "~/App_Data/SitePhotos/JobRequest_" +
                requestId;

            string physicalFolder =
                Server.MapPath(relativeFolder);

            Directory.CreateDirectory(
                physicalFolder);

            string fileName =
                "AFTER_" +
                DateTime.UtcNow.ToString(
                    "yyyyMMdd_HHmmss") +
                "_" +
                Guid.NewGuid().ToString("N") +
                photo.Extension;

            string physicalPath =
                Path.Combine(
                    physicalFolder, fileName);

            string relativePath =
                relativeFolder + "/" + fileName;

            try
            {
                File.WriteAllBytes(
                    physicalPath, photo.Bytes);

                const string sql = @"
                    INSERT INTO dbo.SitePhoto
                    (
                        photoType,
                        filePath,
                        uploadDate,
                        jobID,
                        jobRequestID
                    )
                    VALUES
                    (
                        'AFTER',
                        @path,
                        GETDATE(),
                        @jobID,
                        @requestID
                    );";

                using (SqlCommand command =
                    new SqlCommand(
                        sql, connection, transaction))
                {
                    command.Parameters.Add(
                        "@path", SqlDbType.VarChar, 255)
                        .Value = relativePath;

                    command.Parameters.Add(
                        "@jobID", SqlDbType.Int)
                        .Value = jobId;

                    command.Parameters.Add(
                        "@requestID", SqlDbType.Int)
                        .Value = requestId;

                    command.ExecuteNonQuery();
                }

                return physicalPath;
            }
            catch
            {
                DeleteTemporaryPhoto(physicalPath);
                throw;
            }
        }

        private void DeleteTemporaryPhoto(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) &&
                    File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                LogError("Photo cleanup", ex);
            }
        }

        // ============================================
        // RELEASE OWNED ASSETS ON COMPLETION
        // ============================================

        private void ReleaseOwnedAssets(
            SqlConnection connection,
            SqlTransaction transaction,
            int jobId)
        {
            // Only release owned equipment that is
            // not attached to another active job.
            // Preserve assignment history.

            const string sql = @"
                UPDATE oa

                SET oa.assetStatus = 'Available'

                FROM dbo.OwnedAsset oa

                WHERE oa.assetStatus = 'In Use'

                  AND EXISTS
                  (
                      SELECT 1
                      FROM dbo.JobAssetAssignment ja
                      WHERE ja.ownedAssetID = oa.assetID
                        AND ja.jobID = @jobID
                  )

                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.JobAssetAssignment otherJa

                      INNER JOIN dbo.Job otherJob
                          ON otherJa.jobID = otherJob.jobID

                      WHERE otherJa.ownedAssetID = oa.assetID
                        AND otherJa.jobID <> @jobID
                        AND otherJob.jobStatus IN
                            ('Not Started', 'In Progress')
                  );";

            using (SqlCommand command =
                new SqlCommand(
                    sql, connection, transaction))
            {
                command.Parameters.Add(
                    "@jobID", SqlDbType.Int)
                    .Value = jobId;

                command.ExecuteNonQuery();
            }
        }

        // ============================================
        // SAVE PROGRESS UPDATE
        // ============================================

        protected void btnSaveProgress_Click(
            object sender, EventArgs e)
        {
            int jobId = SelectedJobId;

            if (jobId <= 0 ||
                ddlJobs.SelectedValue != jobId.ToString())
            {
                ShowWarning(
                    "Please load the job before saving.");

                return;
            }

            string newStatus =
                ddlNewStatus.SelectedValue;

            if (!ValidTransition(
                LoadedStatus, newStatus))
            {
                ShowWarning(
                    "Invalid status change. " +
                    "Jobs must progress in order.");

                return;
            }

            PhotoData photo = null;

            try
            {
                photo = ReadCompletionPhoto();

                if (newStatus != "Completed" &&
                    photo != null)
                {
                    ShowWarning(
                        "Completion photos can only be " +
                        "uploaded when completing a job.");

                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                ShowWarning(ex.Message);
                return;
            }

            string savedPhotoPath = null;
            bool committed = false;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (SqlTransaction transaction =
                        connection.BeginTransaction(
                            IsolationLevel.Serializable))
                    {
                        try
                        {
                            string currentStatus;
                            int requestId;

                            // Lock the existing job while
                            // the update is performed.

                            const string selectSql = @"
                                SELECT
                                    j.jobStatus,
                                    q.jobRequestID

                                FROM dbo.Job j
                                    WITH (UPDLOCK, HOLDLOCK)

                                INNER JOIN dbo.Quote q
                                    ON j.quoteID = q.QuoteID

                                WHERE j.jobID = @jobID;";

                            using (SqlCommand command =
                                new SqlCommand(
                                    selectSql,
                                    connection,
                                    transaction))
                            {
                                command.Parameters.Add(
                                    "@jobID", SqlDbType.Int)
                                    .Value = jobId;

                                using (SqlDataReader reader =
                                    command.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new InvalidOperationException(
                                            "The job no longer exists.");
                                    }

                                    currentStatus =
                                        Convert.ToString(
                                            reader["jobStatus"]);

                                    requestId =
                                        Convert.ToInt32(
                                            reader["jobRequestID"]);
                                }
                            }

                            // Someone else may have
                            // updated the job already.

                            if (currentStatus != LoadedStatus)
                            {
                                throw new InvalidOperationException(
                                    "This job was changed by " +
                                    "another user. Reload it first.");
                            }

                            if (!ValidTransition(
                                currentStatus, newStatus))
                            {
                                throw new InvalidOperationException(
                                    "This status change is not allowed.");
                            }

                            // When completing, require a
                            // new or existing AFTER photo.

                            if (newStatus == "Completed")
                            {
                                int existingPhotos;

                                const string photoCountSql = @"
                                    SELECT COUNT(*)
                                    FROM dbo.SitePhoto
                                    WHERE jobID = @jobID
                                      AND photoType = 'AFTER';";

                                using (SqlCommand command =
                                    new SqlCommand(
                                        photoCountSql,
                                        connection,
                                        transaction))
                                {
                                    command.Parameters.Add(
                                        "@jobID", SqlDbType.Int)
                                        .Value = jobId;

                                    existingPhotos =
                                        Convert.ToInt32(
                                            command.ExecuteScalar());
                                }

                                if (existingPhotos == 0 &&
                                    photo == null)
                                {
                                    throw new InvalidOperationException(
                                        "A completion photo is required.");
                                }

                                if (photo != null)
                                {
                                    savedPhotoPath =
                                        SaveCompletionPhoto(
                                            connection,
                                            transaction,
                                            jobId,
                                            requestId,
                                            photo);
                                }
                            }

                            // Save the new status.
                            // Existing startDate is retained.

                            const string updateSql = @"
                                UPDATE dbo.Job

                                SET jobStatus = @newStatus,

                                    startDate =
                                        CASE
                                            WHEN @newStatus =
                                                'In Progress'
                                             AND startDate IS NULL
                                            THEN CONVERT(date, GETDATE())
                                            ELSE startDate
                                        END

                                WHERE jobID = @jobID
                                  AND jobStatus = @oldStatus;";

                            using (SqlCommand command =
                                new SqlCommand(
                                    updateSql,
                                    connection,
                                    transaction))
                            {
                                command.Parameters.Add(
                                    "@jobID", SqlDbType.Int)
                                    .Value = jobId;

                                command.Parameters.Add(
                                    "@newStatus",
                                    SqlDbType.VarChar, 30)
                                    .Value = newStatus;

                                command.Parameters.Add(
                                    "@oldStatus",
                                    SqlDbType.VarChar, 30)
                                    .Value = currentStatus;

                                if (command.ExecuteNonQuery() != 1)
                                {
                                    throw new InvalidOperationException(
                                        "Job status update failed.");
                                }
                            }

                            if (newStatus == "Completed")
                            {
                                ReleaseOwnedAssets(
                                    connection,
                                    transaction,
                                    jobId);
                            }

                            transaction.Commit();
                            committed = true;
                        }
                        catch
                        {
                            if (!committed)
                            {
                                try
                                {
                                    transaction.Rollback();
                                }
                                catch (Exception rollbackEx)
                                {
                                    LogError(
                                        "Rollback",
                                        rollbackEx);
                                }
                            }

                            throw;
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!committed)
                {
                    DeleteTemporaryPhoto(
                        savedPhotoPath);
                }

                ShowWarning(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                if (!committed)
                {
                    DeleteTemporaryPhoto(
                        savedPhotoPath);
                }

                LogError("Save progress", ex);

                ShowError(
                    "Unable to save the progress update. " +
                    "Please reload the job before retrying.");

                return;
            }

            // The transaction has committed.
            // A display error must not undo the photo.

            try
            {
                LoadJobsDropdown();
                LoadSelectedJob(jobId);

                ShowSuccess(
                    "Job #" + jobId +
                    " updated successfully to " +
                    newStatus + ".");
            }
            catch (Exception ex)
            {
                LogError("Refresh after save", ex);

                ShowWarning(
                    "Progress was saved, but the page " +
                    "could not refresh. Reload the page.");
            }
        }

        // ============================================
        // RESET CHANGES
        // ============================================

        protected void btnResetProgress_Click(
            object sender, EventArgs e)
        {
            if (SelectedJobId <= 0)
            {
                ClearJobDetails();
                return;
            }

            try
            {
                LoadSelectedJob(SelectedJobId);

                ShowSuccess(
                    "Unsaved changes have been reset.");
            }
            catch (Exception ex)
            {
                LogError("Reset", ex);

                ClearJobDetails();

                ShowError(
                    "Unable to reload the job.");
            }
        }

        // ============================================
        // CLEAR FORM
        // ============================================

        private void ClearJobDetails()
        {
            SelectedJobId = 0;
            LoadedStatus = "";

            txtJobID.Text = "";
            txtQuoteID.Text = "";
            txtClientName.Text = "";
            txtSiteAddress.Text = "";
            txtScheduledDate.Text = "";
            txtTimeSlots.Text = "";
            txtCurrentStatus.Text = "";
            txtCompletionDate.Text = "";

            lblPhotoInfo.Text = "";

            ddlNewStatus.Items.Clear();

            ddlNewStatus.Items.Add(
                new ListItem(
                    "-- Select next status --", ""));

            ddlNewStatus.Enabled = false;
            btnSaveProgress.Enabled = false;

            pnlCompletion.Visible = false;

            pnlNotStarted.CssClass = "ejp-stage";
            pnlInProgress.CssClass = "ejp-stage";
            pnlCompleted.CssClass = "ejp-stage";
        }
    }
}
