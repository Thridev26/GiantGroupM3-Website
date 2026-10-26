using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace GiantGroupM3
{
    /// <summary>
    /// Time-slot booking rules for the Assign Staff / Assign Asset pages.
    /// Each assignment owns its own slots (JobStaffAssignmentSlot / JobAssetAssignmentSlot), so booking Amit
    /// 08-09 on a job never changes what any other booking covers. Legacy assignments that have no slot rows
    /// fall back to their job's JobTimeSlot rows. Two bookings clash only on the same date AND overlapping slots
    /// (08-09 then 09-10 do not overlap).
    /// </summary>
    public static class Scheduling
    {
        public sealed class Booking
        {
            public int JobId;
            public bool SameJob;
            public List<string> Slots = new List<string>();
            public string Describe()
            {
                string when = string.Join(", ", Slots);
                return SameJob ? "Already on this job (" + when + ")"
                               : "Booked on Job #" + JobId + " (" + when + ")";
            }
        }

        public static string ConnStr
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["GroupWst1ConnectionString"]?.ConnectionString
                    ?? ConfigurationManager.ConnectionStrings["defaultConnection"]?.ConnectionString;
            }
        }

        // ---------------------------------------------------------------- slots
        public static string SlotLabel(object start, object end)
        {
            return ((TimeSpan)start).ToString(@"hh\:mm") + "–" + ((TimeSpan)end).ToString(@"hh\:mm");
        }

        public static DataTable GetTimeSlots()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(ConnStr))
            using (SqlDataAdapter da = new SqlDataAdapter("SELECT timeSlotID, startTime, endTime FROM [dbo].[TimeSlot] ORDER BY startTime", conn))
            {
                da.Fill(dt);
            }
            dt.Columns.Add("Label", typeof(string));
            foreach (DataRow r in dt.Rows) r["Label"] = SlotLabel(r["startTime"], r["endTime"]);
            return dt;
        }

        public static DateTime? GetJobStartDate(int jobId)
        {
            using (SqlConnection conn = new SqlConnection(ConnStr))
            using (SqlCommand cmd = new SqlCommand("SELECT startDate FROM [dbo].[Job] WHERE jobID = @J", conn))
            {
                cmd.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                conn.Open();
                object o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? (DateTime?)null : Convert.ToDateTime(o);
            }
        }

        public static string DescribeSlots(IEnumerable<int> slotIds)
        {
            List<int> ids = slotIds.Distinct().ToList();
            if (ids.Count == 0) return "";
            var labels = GetTimeSlots().AsEnumerable().Where(r => ids.Contains(Convert.ToInt32(r["timeSlotID"])))
                            .OrderBy(r => (TimeSpan)r["startTime"]).Select(r => (string)r["Label"]);
            return string.Join(", ", labels);
        }

        /// <summary>Only ever interpolates integers we parsed ourselves, never user text.</summary>
        public static string InList(IEnumerable<int> ids)
        {
            return string.Join(",", ids.Distinct().Select(i => i.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>SQL fragment: the booked slot (alias ts) overlaps any of the chosen slots.</summary>
        public static string OverlapSql(IEnumerable<int> chosenSlotIds)
        {
            return "EXISTS (SELECT 1 FROM [dbo].[TimeSlot] sel WHERE sel.timeSlotID IN (" + InList(chosenSlotIds) + ")"
                 + " AND ts.startTime < sel.endTime AND ts.endTime > sel.startTime)";
        }

        /// <summary>Validates ids against the TimeSlot table (they come from a posted checkbox list).</summary>
        public static bool AllSlotsExist(List<int> slotIds)
        {
            if (slotIds == null || slotIds.Count == 0) return false;
            return GetTimeSlots().AsEnumerable().Count(r => slotIds.Contains(Convert.ToInt32(r["timeSlotID"]))) == slotIds.Distinct().Count();
        }

        // ------------------------------------------------------------ bookings
        // Effective slots of an assignment = its own slot rows, or (legacy rows) the job's slots.
        private const string StaffFrom = @"
            FROM [dbo].[JobStaffAssignment] a
            JOIN [dbo].[TimeSlot] ts ON ts.timeSlotID IN (
                SELECT s.timeSlotID FROM [dbo].[JobStaffAssignmentSlot] s WHERE s.staffID = a.staffID AND s.jobID = a.jobID
                UNION ALL
                SELECT jts.timeSlotID FROM [dbo].[JobTimeSlot] jts
                WHERE jts.jobID = a.jobID
                  AND NOT EXISTS (SELECT 1 FROM [dbo].[JobStaffAssignmentSlot] s2 WHERE s2.staffID = a.staffID AND s2.jobID = a.jobID)) ";

        private const string AssetFrom = @"
            FROM [dbo].[JobAssetAssignment] a
            JOIN [dbo].[TimeSlot] ts ON ts.timeSlotID IN (
                SELECT s.timeSlotID FROM [dbo].[JobAssetAssignmentSlot] s WHERE s.assignmentID = a.AssignmentID
                UNION ALL
                SELECT jts.timeSlotID FROM [dbo].[JobTimeSlot] jts
                WHERE jts.jobID = a.jobID
                  AND NOT EXISTS (SELECT 1 FROM [dbo].[JobAssetAssignmentSlot] s2 WHERE s2.assignmentID = a.AssignmentID)) ";

        private const string AssetKeySql = @"CASE WHEN a.ownedAssetID IS NOT NULL THEN 'Owned:' + CAST(a.ownedAssetID AS varchar(20))
                            ELSE 'Hired:' + CAST(a.hiredAssetID AS varchar(20)) END";

        private static Dictionary<string, Booking> RunBookingQuery(string sql, DateTime date, int jobId,
            SqlConnection conn, SqlTransaction tx, Action<SqlCommand> addParams)
        {
            var result = new Dictionary<string, Booking>();
            using (SqlCommand cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.Add("@D", SqlDbType.Date).Value = date.Date;
                if (addParams != null) addParams(cmd);
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string key = dr.GetString(0);
                        int bookedJob = dr.GetInt32(1);
                        bool same = bookedJob == jobId;
                        Booking b;
                        // A clash with a different job matters more than "already on this job".
                        if (!result.TryGetValue(key, out b) || (b.SameJob && !same))
                        {
                            b = new Booking { JobId = bookedJob, SameJob = same };
                            result[key] = b;
                        }
                        if (b.JobId == bookedJob)
                        {
                            string label = SlotLabel(dr.GetValue(2), dr.GetValue(3));
                            if (!b.Slots.Contains(label)) b.Slots.Add(label);
                        }
                    }
                }
            }
            return result;
        }

        private static Dictionary<string, Booking> Bookings(string from, string keySql, DateTime date, int jobId, List<int> slotIds,
            SqlConnection conn, SqlTransaction tx, string extraWhere, Action<SqlCommand> addParams)
        {
            if (slotIds == null || slotIds.Count == 0) return new Dictionary<string, Booking>();
            string sql = "SELECT " + keySql + ", a.jobID, ts.startTime, ts.endTime " + from
                       + " WHERE a.assignmentDate = @D " + extraWhere + " AND " + OverlapSql(slotIds) + " ORDER BY ts.startTime";
            if (conn != null) return RunBookingQuery(sql, date, jobId, conn, tx, addParams);
            using (SqlConnection c = new SqlConnection(ConnStr))
            {
                c.Open();
                return RunBookingQuery(sql, date, jobId, c, null, addParams);
            }
        }

        /// <summary>Key = staffID as text.</summary>
        public static Dictionary<string, Booking> GetStaffBookings(DateTime date, int jobId, List<int> slotIds)
        {
            return Bookings(StaffFrom, "CAST(a.staffID AS varchar(20))", date, jobId, slotIds, null, null, "", null);
        }

        /// <summary>Key = "Owned:5" or "Hired:2".</summary>
        public static Dictionary<string, Booking> GetAssetBookings(DateTime date, int jobId, List<int> slotIds)
        {
            return Bookings(AssetFrom, AssetKeySql, date, jobId, slotIds, null, null, "", null);
        }

        // ------------------------------------------------------------- assigning
        private static string FriendlyError(SqlException ex)
        {
            if (ex.Number == 208)
                return "The slot tables are missing from the database. Run Add_Assignment_Slot_Tables.sql first.";
            return null;
        }

        /// <summary>Books a staff member onto a job for the given date + slots. Returns an error message, or null on success.</summary>
        public static string AssignStaff(int staffId, int jobId, DateTime date, List<int> slotIds)
        {
            if (!AllSlotsExist(slotIds)) return "Choose at least one valid time slot.";
            slotIds = slotIds.Distinct().ToList();
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnStr))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            // One assignment row per person per job (primary key). Same date = just add more slots to it.
                            bool haveRow = false;
                            using (SqlCommand ex = new SqlCommand("SELECT assignmentDate FROM [dbo].[JobStaffAssignment] WHERE staffID = @S AND jobID = @J", conn, tx))
                            {
                                ex.Parameters.Add("@S", SqlDbType.Int).Value = staffId;
                                ex.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                                object existing = ex.ExecuteScalar();
                                if (existing != null && existing != DBNull.Value)
                                {
                                    if (Convert.ToDateTime(existing).Date != date.Date)
                                    {
                                        tx.Rollback();
                                        return "This staff member is already on Job #" + jobId + " on "
                                            + Convert.ToDateTime(existing).ToString("yyyy-MM-dd") + ". A person can only be linked to a job on one date.";
                                    }
                                    haveRow = true;
                                }
                            }

                            var clash = Bookings(StaffFrom, "CAST(a.staffID AS varchar(20))", date, jobId, slotIds, conn, tx,
                                " AND a.staffID = @R", c => c.Parameters.Add("@R", SqlDbType.Int).Value = staffId);
                            Booking b;
                            if (clash.TryGetValue(staffId.ToString(), out b))
                            {
                                tx.Rollback();
                                return "Can't assign: " + b.Describe() + " on " + date.ToString("yyyy-MM-dd")
                                     + ". Pick someone else or different slots.";
                            }

                            if (!haveRow)
                            {
                                using (SqlCommand ins = new SqlCommand(
                                    "INSERT INTO [dbo].[JobStaffAssignment] (staffID, jobID, hoursWorked, assignmentDate) VALUES (@S, @J, 0, @D)", conn, tx))
                                {
                                    ins.Parameters.Add("@S", SqlDbType.Int).Value = staffId;
                                    ins.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                                    ins.Parameters.Add("@D", SqlDbType.Date).Value = date.Date;
                                    ins.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Legacy row with no slot rows of its own: keep what it used to cover (the job's slots).
                                using (SqlCommand keep = new SqlCommand(@"
                                    INSERT INTO [dbo].[JobStaffAssignmentSlot] (staffID, jobID, timeSlotID)
                                    SELECT @S, @J, jts.timeSlotID FROM [dbo].[JobTimeSlot] jts
                                    WHERE jts.jobID = @J
                                      AND NOT EXISTS (SELECT 1 FROM [dbo].[JobStaffAssignmentSlot] s WHERE s.staffID = @S AND s.jobID = @J)", conn, tx))
                                {
                                    keep.Parameters.Add("@S", SqlDbType.Int).Value = staffId;
                                    keep.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                                    keep.ExecuteNonQuery();
                                }
                            }

                            foreach (int slot in slotIds)
                            {
                                using (SqlCommand sc = new SqlCommand(@"
                                    IF NOT EXISTS (SELECT 1 FROM [dbo].[JobStaffAssignmentSlot] WHERE staffID = @S AND jobID = @J AND timeSlotID = @T)
                                        INSERT INTO [dbo].[JobStaffAssignmentSlot] (staffID, jobID, timeSlotID) VALUES (@S, @J, @T)", conn, tx))
                                {
                                    sc.Parameters.Add("@S", SqlDbType.Int).Value = staffId;
                                    sc.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                                    sc.Parameters.Add("@T", SqlDbType.Int).Value = slot;
                                    sc.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand hrs = new SqlCommand(@"
                                UPDATE [dbo].[JobStaffAssignment]
                                SET hoursWorked = ISNULL((SELECT SUM(DATEDIFF(MINUTE, ts.startTime, ts.endTime)) / 60.0
                                                          FROM [dbo].[JobStaffAssignmentSlot] s
                                                          JOIN [dbo].[TimeSlot] ts ON ts.timeSlotID = s.timeSlotID
                                                          WHERE s.staffID = @S AND s.jobID = @J), 0)
                                WHERE staffID = @S AND jobID = @J", conn, tx))
                            {
                                hrs.Parameters.Add("@S", SqlDbType.Int).Value = staffId;
                                hrs.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                                hrs.ExecuteNonQuery();
                            }

                            tx.Commit();
                            return null;
                        }
                        catch { try { tx.Rollback(); } catch { } throw; }
                    }
                }
            }
            catch (SqlException ex)
            {
                string friendly = FriendlyError(ex);
                if (friendly != null) return friendly;
                throw;
            }
        }

        /// <summary>Books an asset onto a job for the given date + slots. Returns an error message, or null on success.</summary>
        public static string AssignAsset(bool owned, int assetId, int jobId, DateTime date, List<int> slotIds)
        {
            if (!AllSlotsExist(slotIds)) return "Choose at least one valid time slot.";
            slotIds = slotIds.Distinct().ToList();
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnStr))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            var clash = Bookings(AssetFrom, AssetKeySql, date, jobId, slotIds, conn, tx,
                                owned ? " AND a.ownedAssetID = @R" : " AND a.hiredAssetID = @R",
                                c => c.Parameters.Add("@R", SqlDbType.Int).Value = assetId);
                            Booking b;
                            if (clash.TryGetValue((owned ? "Owned:" : "Hired:") + assetId, out b))
                            {
                                tx.Rollback();
                                return "Can't assign: " + b.Describe() + " on " + date.ToString("yyyy-MM-dd")
                                     + ". Pick a different asset or different slots.";
                            }

                            int assignmentId;
                            using (SqlCommand ins = new SqlCommand(@"
                                INSERT INTO [dbo].[JobAssetAssignment] (jobID, ownedAssetID, hiredAssetID, assignmentDate)
                                VALUES (@J, @O, @H, @D);
                                SELECT CAST(SCOPE_IDENTITY() AS int);", conn, tx))
                            {
                                ins.Parameters.Add("@J", SqlDbType.Int).Value = jobId;
                                ins.Parameters.Add("@O", SqlDbType.Int).Value = owned ? (object)assetId : DBNull.Value;
                                ins.Parameters.Add("@H", SqlDbType.Int).Value = owned ? DBNull.Value : (object)assetId;
                                ins.Parameters.Add("@D", SqlDbType.Date).Value = date.Date;
                                assignmentId = Convert.ToInt32(ins.ExecuteScalar());
                            }

                            foreach (int slot in slotIds)
                            {
                                using (SqlCommand sc = new SqlCommand(
                                    "INSERT INTO [dbo].[JobAssetAssignmentSlot] (assignmentID, timeSlotID) VALUES (@A, @T)", conn, tx))
                                {
                                    sc.Parameters.Add("@A", SqlDbType.Int).Value = assignmentId;
                                    sc.Parameters.Add("@T", SqlDbType.Int).Value = slot;
                                    sc.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            return null;
                        }
                        catch { try { tx.Rollback(); } catch { } throw; }
                    }
                }
            }
            catch (SqlException ex)
            {
                string friendly = FriendlyError(ex);
                if (friendly != null) return friendly;
                throw;
            }
        }

        // ----------------------------------------------------------- html helpers
        public static string StatusHtml(Booking b)
        {
            if (b == null)
                return "<span class='gg-chip is-done'>Free for these slots</span>";
            return "<span class='gg-chip is-pending'>" + System.Web.HttpUtility.HtmlEncode(b.Describe()) + "</span>";
        }
    }
}
