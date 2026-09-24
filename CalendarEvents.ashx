<%@ WebHandler Language="C#" Class="GiantGroupM3.CalendarEvents" %>

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

namespace GiantGroupM3
{
    public class CalendarEvents : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                context.Response.Write("[]");
                return;
            }

            // 1. Auto-update job statuses safely
            AutoUpdateJobStatuses(connString);

            // 2. Read parameters for calendar display
            string start = context.Request.QueryString["start"];
            string end = context.Request.QueryString["end"];

            DateTime startDate = string.IsNullOrEmpty(start) ? new DateTime(2024, 1, 1) : DateTime.Parse(start);
            DateTime endDate = string.IsNullOrEmpty(end) ? new DateTime(2028, 1, 1) : DateTime.Parse(end);

            var eventList = new List<object>();

            // Query retrieves active jobs + inspection dispatches
            string query = @"
                SELECT 'Job' AS EventType, 
                       j.jobID AS EventID, 
                       ('Job #' + CAST(j.jobID AS VARCHAR) + ' - ' + ISNULL(j.jobStatus, '')) AS EventTitle, 
                       j.startDate AS EventDate
                FROM Job j
                WHERE j.startDate IS NOT NULL
                  AND j.startDate >= @StartDate AND j.startDate < @EndDate
                  AND ISNULL(j.jobStatus, '') NOT IN ('Archived')

                UNION ALL

                SELECT 'Assignment' AS EventType, 
                       js.jobID AS EventID, 
                       ('Inspection: ' + ISNULL(s.firstName, '') + ' ' + ISNULL(s.lastName, '')) AS EventTitle, 
                       js.assignmentDate AS EventDate
                FROM JobStaffAssignment js
                LEFT JOIN Staff s ON js.staffID = s.staffID
                WHERE js.assignmentDate IS NOT NULL
                  AND js.assignmentDate >= @StartDate AND js.assignmentDate < @EndDate";

            using (SqlConnection conn = new SqlConnection(connString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string eventType = reader["EventType"].ToString();
                        object id = reader["EventID"];
                        string title = reader["EventTitle"].ToString();
                        DateTime date = Convert.ToDateTime(reader["EventDate"]);

                        bool isJob = string.Equals(eventType, "Job", StringComparison.OrdinalIgnoreCase);

                        eventList.Add(new
                        {
                            id = id.ToString(),
                            title = title,
                            start = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                            url = isJob 
                                ? VirtualPathUtility.ToAbsolute(string.Format("~/Jobs/ManageJob.aspx?jobID={0}", id))
                                : VirtualPathUtility.ToAbsolute(string.Format("~/Requests/SitePhotos.aspx?requestID={0}", id)),
                            backgroundColor = isJob ? "#0d6efd" : "#198754",
                            borderColor = isJob ? "#0d6efd" : "#198754",
                            textColor = "#ffffff",
                            extendedProps = new { type = eventType }
                        });
                    }
                }
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            context.Response.Write(serializer.Serialize(eventList));
        }

        private void AutoUpdateJobStatuses(string connString)
        {
            string updateQuery = @"
        -- 1. Any job scheduled for TODAY becomes 'In Progress'
        UPDATE Job
        SET jobStatus = 'In Progress'
        WHERE startDate IS NOT NULL
          AND CAST(startDate AS DATE) = CAST(GETDATE() AS DATE)
          AND (jobStatus = 'Not Started' OR jobStatus = 'Pending' OR jobStatus IS NULL);

        -- 2. Any job scheduled BEFORE TODAY automatically becomes 'Completed'
        UPDATE Job
        SET jobStatus = 'Completed'
        WHERE startDate IS NOT NULL
          AND CAST(startDate AS DATE) < CAST(GETDATE() AS DATE)
          AND jobStatus IN ('Not Started', 'Pending', 'In Progress', '');
    ";

    try
    {
        using (SqlConnection conn = new SqlConnection(connString))
        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
        {
            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }
    catch (SqlException ex)
    {
        // Log the exception to the system event log or diagnostic console so execution isn't interrupted
        System.Diagnostics.Debug.WriteLine("Error auto-updating job statuses: " + ex.Message);
    }
    catch (Exception ex)
    {
        // Catch any general unexpected runtime exceptions
        System.Diagnostics.Debug.WriteLine("General error in AutoUpdateJobStatuses: " + ex.Message);
    }
        }

        public bool IsReusable => false;
    }
}