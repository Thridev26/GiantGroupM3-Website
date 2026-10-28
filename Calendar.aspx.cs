using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Web.Script.Serialization;

namespace GiantGroupM3
{
    /// <summary>
    /// "My Schedule" calendar. The signed-in worker (Session["StaffID"], set at login) sees their own jobs
    /// first; Operations Manager and above can also view the whole team and site evaluations.
    /// </summary>
    public partial class CalendarPage : System.Web.UI.Page
    {
        private const int TeamAccessLevel = 4;

        /// <summary>First name used in the greeting (HTML-encoded in the markup).</summary>
        public string FirstName = "there";
        public string FullName = "";
        public string RoleName = "";
        public bool CanSeeTeam;

        /// <summary>Small JSON blob read by the page script (API url and permissions only, no user text).</summary>
        public string ConfigJson = "{}";

        protected void Page_Load(object sender, EventArgs e)
        {
            int staffId;
            object sid = Session["StaffID"];
            if (sid == null || !int.TryParse(sid.ToString(), out staffId) || staffId <= 0)
            {
                // Clients and signed-out visitors never see the staff calendar.
                Response.Redirect("~/Account/Login.aspx", true);
                return;
            }

            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"] != null
                ? ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString
                : null;

            int level = 0;
            using (var conn = new SqlConnection(connString))
            using (var cmd = new SqlCommand(@"
                SELECT s.firstName, s.lastName, r.roleName, r.accessLevel
                FROM dbo.Staff s
                INNER JOIN dbo.Role r ON r.roleID = s.roleID
                WHERE s.staffID = @id AND RTRIM(LTRIM(ISNULL(s.staffStatus, ''))) = 'Active'", conn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = staffId;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read())
                    {
                        Response.Redirect("~/Account/Login.aspx", true);
                        return;
                    }

                    string first = Str(r["firstName"]).Trim();
                    string last = Str(r["lastName"]).Trim();
                    FirstName = first.Length > 0 ? first : "there";
                    FullName = (first + " " + last).Trim();
                    RoleName = Str(r["roleName"]).Trim();
                    level = Convert.ToInt32(r["accessLevel"]);
                }
            }

            CanSeeTeam = level >= TeamAccessLevel;
            ConfigJson = new JavaScriptSerializer().Serialize(new Dictionary<string, object>
            {
                { "apiUrl", ResolveUrl("~/CalendarEvents.ashx") },
                { "canSeeTeam", CanSeeTeam },
                { "level", level }
            });
        }

        private static string Str(object o)
        {
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
        }
    }
}
