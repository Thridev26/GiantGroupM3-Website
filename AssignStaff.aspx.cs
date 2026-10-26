using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace GiantGroupM3
{
    public partial class AssignStaff : Page
    {
        private string connStr = ConfigurationManager.ConnectionStrings["GroupWst1ConnectionString"]?.ConnectionString
            ?? ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

        // Trusted copy of the selected job (ViewState is tamper-proof; hidden fields are not)
        private int? SelectedJobId
        {
            get { return ViewState["SelectedJobId"] as int?; }
            set { ViewState["SelectedJobId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();

            if (!IsUserAuthorized())
            {
                Response.Redirect("~/Account/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindJobsGrid();
                BindStaffGrid();
            }
        }

        // ------------------------------------------------------------
        // Authorisation (levels 5 / 6 or roles 10 / 11 only)
        // ------------------------------------------------------------
        private bool IsUserAuthorized()
        {
            if (!Request.IsAuthenticated) return false;

            int level = 0;
            int roleId = 0;

            if (Session["AccessLevel"] != null)
                int.TryParse(Session["AccessLevel"].ToString(), out level);
            else if (Session["accessLevel"] != null)
                int.TryParse(Session["accessLevel"].ToString(), out level);

            if (Session["RoleID"] != null)
                int.TryParse(Session["RoleID"].ToString(), out roleId);
            else if (Session["roleID"] != null)
                int.TryParse(Session["roleID"].ToString(), out roleId);

            if (level == 5 || level == 6 || roleId == 10 || roleId == 11)
            {
                return true;
            }

            if (Session["StaffID"] != null || Session["staffID"] != null || Session["UserName"] != null || Session["userName"] != null)
            {
                return CheckUserRoleFromDatabase();
            }

            return false;
        }

        private bool CheckUserRoleFromDatabase()
        {
            object staffIdObj = Session["StaffID"] ?? Session["staffID"];
            object userNameObj = Session["UserName"] ?? Session["userName"];

            bool byId = staffIdObj != null && int.TryParse(staffIdObj.ToString(), out int staffId);
            if (!byId && userNameObj == null) return false;

            string query = @"
                SELECT r.accessLevel, s.roleID 
                FROM [dbo].[Staff] s
                INNER JOIN [dbo].[Role] r ON s.roleID = r.roleID
                WHERE " + (byId ? "s.staffID = @ID" : "s.userName = @IDStr");

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (byId) cmd.Parameters.Add("@ID", SqlDbType.Int).Value = int.Parse(staffIdObj.ToString());
                else cmd.Parameters.Add("@IDStr", SqlDbType.NVarChar, 100).Value = userNameObj.ToString();

                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        int accessLevel = dr["accessLevel"] != DBNull.Value ? Convert.ToInt32(dr["accessLevel"]) : 0;
                        int roleId = dr["roleID"] != DBNull.Value ? Convert.ToInt32(dr["roleID"]) : 0;
                        return accessLevel == 5 || accessLevel == 6 || roleId == 10 || roleId == 11;
                    }
                }
            }

            return false;
        }

        // ------------------------------------------------------------
        // Data binding
        // ------------------------------------------------------------
        private static string LikeTerm(string s)
        {
            return "%" + s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        }

        private void BindJobsGrid()
        {
            string searchTerm = txtSearchJobs.Text.Trim();

            string query = @"
                SELECT 
                    j.jobID, 
                    j.startDate,
                    ISNULL(c.clientName + ' ' + c.clientSurname, 'N/A') AS ClientName,
                    ISNULL(jr.siteAddress, 'N/A') AS siteAddress
                FROM [dbo].[Job] j
                LEFT JOIN [dbo].[Quote] q ON j.quoteID = q.quoteID
                LEFT JOIN [dbo].[JobRequest] jr ON q.jobRequestID = jr.jobRequestID
                LEFT JOIN [dbo].[Client] c ON jr.clientID = c.clientID
                WHERE j.jobStatus != 'Archived' ";

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += @" AND (
                    CAST(j.jobID AS VARCHAR) LIKE @Search OR 
                    c.clientName LIKE @Search OR 
                    c.clientSurname LIKE @Search OR 
                    jr.siteAddress LIKE @Search
                ) ";
            }

            query += " ORDER BY j.jobID ASC";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", LikeTerm(searchTerm));
                }

                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    gvJobs.DataSource = dt;
                    gvJobs.DataBind();
                }
            }
        }

        // Exclude System Managers (Level 5 / Role 10) and Owners (Level 6 / Role 11)
        private const string AssignableStaffFilter = @"
                s.staffStatus = 'Active'
                AND (r.accessLevel NOT IN (5, 6) OR r.accessLevel IS NULL)
                AND (s.roleID NOT IN (10, 11) OR s.roleID IS NULL) ";

        private void BindStaffGrid()
        {
            string searchTerm = txtSearchStaff.Text.Trim();

            string query = @"
                SELECT 
                    s.staffID, 
                    s.firstName,
                    s.lastName,
                    s.contactNumber,
                    s.emailAddress,
                    ISNULL(r.roleName, 'General') AS roleName
                FROM [dbo].[Staff] s
                LEFT JOIN [dbo].[Role] r ON s.roleID = r.roleID
                WHERE " + AssignableStaffFilter;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += @" AND (
                    s.firstName LIKE @Search OR 
                    s.lastName LIKE @Search OR 
                    s.emailAddress LIKE @Search OR 
                    s.contactNumber LIKE @Search OR 
                    r.roleName LIKE @Search
                ) ";
            }

            query += " ORDER BY s.firstName ASC";

            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@Search", LikeTerm(searchTerm));
                }

                conn.Open();
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // Mark each person free / booked for the applied date + the job's time slots
                    dt.Columns.Add("IsBooked", typeof(bool));
                    dt.Columns.Add("StatusHtml", typeof(string));
                    Dictionary<string, Scheduling.Booking> bookings = CurrentBookings();
                    foreach (DataRow r in dt.Rows)
                    {
                        if (bookings == null)
                        {
                            r["IsBooked"] = true;
                            r["StatusHtml"] = SelectedJobId.HasValue
                                ? "<span class='gg-chip'>Choose date &amp; slots in step 2</span>"
                                : "<span class='gg-chip'>Select a job first</span>";
                            continue;
                        }
                        Scheduling.Booking b;
                        bookings.TryGetValue(Convert.ToString(r["staffID"]), out b);
                        r["IsBooked"] = b != null;
                        r["StatusHtml"] = Scheduling.StatusHtml(b);
                    }

                    DataView view = dt.DefaultView;
                    if (chkHideBooked.Checked && bookings != null) view.RowFilter = "IsBooked = false";
                    gvStaff.DataSource = view;
                    gvStaff.DataBind();
                }
            }
        }

        // Assign buttons stay disabled until a job has been chosen
        protected void gvStaff_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var btn = e.Row.FindControl("btnAssignStaff") as LinkButton;
                DataRowView drv = e.Row.DataItem as DataRowView;
                bool free = drv != null && !(bool)drv["IsBooked"];
                if (btn != null) btn.Enabled = SelectedJobId.HasValue && free;
            }
        }


        // ------------------------------------------------------------
        // Step 2: date + time slots for THIS booking
        // ------------------------------------------------------------
        // The date and slots the availability grid is checked against, and that the next Assign will book.
        // Held in ViewState (tamper-proof). They belong to this booking only: they are never written to the job.
        private DateTime? AppliedDate
        {
            get { return ViewState["AppliedDate"] as DateTime?; }
            set { ViewState["AppliedDate"] = value; }
        }

        private List<int> AppliedSlots
        {
            get { int[] a = ViewState["AppliedSlots"] as int[]; return a == null ? new List<int>() : a.ToList(); }
            set { ViewState["AppliedSlots"] = value == null ? null : value.ToArray(); }
        }

        private void InitSchedule(int jobId)
        {
            cblSlots.DataSource = Scheduling.GetTimeSlots();
            cblSlots.DataBind();
            foreach (ListItem li in cblSlots.Items) li.Selected = false;

            DateTime date = Scheduling.GetJobStartDate(jobId) ?? DateTime.Today;
            txtWorkDate.Text = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            AppliedDate = null;
            AppliedSlots = null;
            pnlSchedule.Visible = true;
            UpdateScheduleSummary();
        }

        private void UpdateScheduleSummary()
        {
            List<int> slots = AppliedSlots;
            if (!AppliedDate.HasValue || slots.Count == 0)
            {
                lblScheduleSummary.Text = "Pick a date and the slots you want to book for, then press <strong>Check availability</strong>.";
                return;
            }
            lblScheduleSummary.Text = "Checking <strong>" + Server.HtmlEncode(AppliedDate.Value.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture))
                + "</strong> &middot; " + Server.HtmlEncode(Scheduling.DescribeSlots(slots));
        }

        protected void btnApplySchedule_Click(object sender, EventArgs e)
        {
            if (!SelectedJobId.HasValue)
            {
                ShowMessage("Please select a job from Step 1 first.", "gg-msg is-warning");
                return;
            }

            DateTime date;
            if (!DateTime.TryParseExact(txtWorkDate.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                ShowMessage("Please enter a valid work date.", "gg-msg is-warning");
                return;
            }

            List<int> chosen = cblSlots.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)).ToList();
            if (!Scheduling.AllSlotsExist(chosen))
            {
                ShowMessage("Tick at least one time slot.", "gg-msg is-warning");
                return;
            }

            AppliedDate = date;
            AppliedSlots = chosen;
            UpdateScheduleSummary();
            gvStaff.PageIndex = 0;
            BindStaffGrid();
            ShowMessage("Availability updated for the slots you ticked. Anything already booked in those slots on that day is locked.", "gg-msg is-info");
        }

        protected void chkHideBooked_CheckedChanged(object sender, EventArgs e)
        {
            gvStaff.PageIndex = 0;
            BindStaffGrid();
        }

        /// <summary>Bookings for the applied date + slots, or null when no schedule has been applied yet.</summary>
        private Dictionary<string, Scheduling.Booking> CurrentBookings()
        {
            if (!SelectedJobId.HasValue || !AppliedDate.HasValue) return null;
            List<int> slots = AppliedSlots;
            if (slots.Count == 0) return null;
            return Scheduling.GetStaffBookings(AppliedDate.Value, SelectedJobId.Value, slots);
        }

        // ------------------------------------------------------------
        // Search handlers
        // ------------------------------------------------------------
        protected void txtSearchJobs_TextChanged(object sender, EventArgs e)
        {
            gvJobs.PageIndex = 0;
            BindJobsGrid();
        }

        protected void btnSearchJobs_Click(object sender, EventArgs e)
        {
            gvJobs.PageIndex = 0;
            BindJobsGrid();
        }

        protected void txtSearchStaff_TextChanged(object sender, EventArgs e)
        {
            gvStaff.PageIndex = 0;
            BindStaffGrid();
        }

        protected void btnSearchStaff_Click(object sender, EventArgs e)
        {
            gvStaff.PageIndex = 0;
            BindStaffGrid();
        }

        // ------------------------------------------------------------
        // Step 1: choose a job
        // ------------------------------------------------------------
        protected void gvJobs_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "SelectJob") return;

            int jobId;
            if (!int.TryParse(e.CommandArgument.ToString(), out jobId) || !JobIsOpen(jobId))
            {
                ShowMessage("That job is no longer available. Please pick another one.", "gg-msg is-warning");
                BindJobsGrid();
                return;
            }

            SelectedJobId = jobId;
            hfSelectedJobID.Value = jobId.ToString();

            foreach (GridViewRow row in gvJobs.Rows)
            {
                if (gvJobs.DataKeys[row.RowIndex].Value.ToString() == jobId.ToString())
                {
                    // BoundField cell text is already HTML-encoded by ASP.NET
                    string client = row.Cells[1].Text;
                    string address = row.Cells[2].Text;
                    litSelectedJobDetails.Text = $"Job #{jobId} - {client} ({address})";
                    break;
                }
            }

            pnlSelectedJob.CssClass = "gg-selected";
            pnlJobsGrid.Visible = false;
            InitSchedule(jobId);
            BindStaffGrid(); // re-bind so the Assign buttons unlock
            ShowMessage("Job selected! Now choose its date and time slots (step 2), then check availability.", "gg-msg is-info");
        }

        protected void btnClearJob_Click(object sender, EventArgs e)
        {
            SelectedJobId = null;
            hfSelectedJobID.Value = string.Empty;
            pnlSelectedJob.CssClass = "gg-selected d-none";
            pnlJobsGrid.Visible = true;
            pnlSchedule.Visible = false;
            AppliedDate = null;
            lblMessage.CssClass = "d-none";
            BindJobsGrid();
            BindStaffGrid(); // re-lock the Assign buttons
        }

        private bool JobIsOpen(int jobId)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM [dbo].[Job] WHERE jobID = @JobID AND jobStatus != 'Archived'", conn))
            {
                cmd.Parameters.Add("@JobID", SqlDbType.Int).Value = jobId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // ------------------------------------------------------------
        // Step 2: assign a staff member
        // ------------------------------------------------------------
        protected void gvStaff_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "AssignStaffMember") return;

            if (!SelectedJobId.HasValue)
            {
                ShowMessage("Please select a job from Step 1 first.", "gg-msg is-warning");
                return;
            }

            int staffId;
            if (!int.TryParse(e.CommandArgument.ToString(), out staffId))
            {
                ShowMessage("That staff member could not be read. Please refresh the page and try again.", "gg-msg is-error");
                return;
            }

            int jobId = SelectedJobId.Value;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // Staff member must still be assignable (active, not an owner / system manager)
                    using (SqlCommand check = new SqlCommand(@"
                        SELECT COUNT(1) FROM [dbo].[Staff] s
                        LEFT JOIN [dbo].[Role] r ON s.roleID = r.roleID
                        WHERE s.staffID = @S AND " + AssignableStaffFilter, conn))
                    {
                        check.Parameters.Add("@S", SqlDbType.Int).Value = staffId;
                        if (Convert.ToInt32(check.ExecuteScalar()) == 0)
                        {
                            ShowMessage("That staff member can't be assigned to jobs.", "gg-msg is-warning");
                            BindStaffGrid();
                            return;
                        }
                    }

                    // Date + slots come from step 2 (this booking only)
                    List<int> slots = AppliedSlots;
                    if (!AppliedDate.HasValue || slots.Count == 0)
                    {
                        ShowMessage("Choose a date and time slots in step 2 first.", "gg-msg is-warning");
                        return;
                    }
                    DateTime date = AppliedDate.Value;

                    // Clash check + insert happen together in one serializable transaction
                    string error = Scheduling.AssignStaff(staffId, jobId, date, slots);
                    if (error != null)
                    {
                        ShowMessage(error, "gg-msg is-warning");
                        BindStaffGrid();
                        return;
                    }
                }

                ShowMessage("Staff member assigned to Job #" + jobId + " on " + AppliedDate.Value.ToString("yyyy-MM-dd") + " (" + Scheduling.DescribeSlots(AppliedSlots) + ").", "gg-msg is-success");
                BindStaffGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("AssignStaff failed: " + ex);
                ShowMessage("We couldn't save that assignment. Please try again.", "gg-msg is-error");
            }
        }

        private void ShowMessage(string message, string cssClass)
        {
            lblMessage.Text = Server.HtmlEncode(message);
            lblMessage.CssClass = cssClass;
        }

        protected void gvJobs_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvJobs.PageIndex = e.NewPageIndex;
            BindJobsGrid();
        }

        protected void gvStaff_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvStaff.PageIndex = e.NewPageIndex;
            BindStaffGrid();
        }
    }
}