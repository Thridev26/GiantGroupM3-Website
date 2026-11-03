<%@ WebHandler Language="C#" Class="GiantGroupM3.GalleryImageHandler" %>
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web;

namespace GiantGroupM3
{
    public class GalleryImageHandler : IHttpHandler
    {
        public bool IsReusable { get { return false; } }
        public void ProcessRequest(HttpContext context)
        {
            int id;
            string side = context.Request.QueryString["side"];
            if (!int.TryParse(context.Request.QueryString["id"], out id) || id <= 0 ||
                (side != "before" && side != "after"))
            { context.Response.StatusCode = 404; return; }
            string mimeColumn = side == "before" ? "BeforeMime" : "AfterMime";
            string imageColumn = side == "before" ? "BeforeImage" : "AfterImage";
            using (var cn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var cmd = new SqlCommand("SELECT " + mimeColumn + ", " + imageColumn + " FROM dbo.PublicJobGallery WHERE GalleryID=@id AND IsPublished=1", cn))
            {
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) { context.Response.StatusCode = 404; return; }
                    string mime = reader.GetString(0);
                    if (mime != "image/jpeg" && mime != "image/png") { context.Response.StatusCode = 404; return; }
                    byte[] data = (byte[])reader[1];
                    context.Response.ContentType = mime;
                    context.Response.Cache.SetCacheability(HttpCacheability.Public);
                    context.Response.Cache.SetMaxAge(TimeSpan.FromMinutes(5));
                    context.Response.AddHeader("X-Content-Type-Options", "nosniff");
                    context.Response.BinaryWrite(data);
                }
            }
        }
    }
}
