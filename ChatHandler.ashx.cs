using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using GiantGroupM3.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GiantGroupM3
{
    /// <summary>
    /// Oak chatbot endpoint.
    /// Request  (POST, JSON): { "message": "...", "history": [ { "role": "user"|"model", "text": "..." } ] }
    /// Response (always HTTP 200): { "ok": true|false, "reply": "...", "mood": "happy", "reason": "limit"|null }
    /// When ok is false the page falls back to the keyword answers, so the chat never breaks.
    /// </summary>
    public class ChatHandler : IHttpHandler, IReadOnlySessionState
    {
        // ---- limits: adjust once you see your real quota on the AI Studio rate-limit page ----
        private const int MaxMessageLength = 300;
        private const int MaxHistoryTurns = 6;
        private const int MaxTurnLength = 600;
        private const int GuestPerTenMinutes = 8;
        private const int MemberPerTenMinutes = 20;
        private const int GlobalPerDay = 400;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);

            // 1. POST only
            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                Send(context, false);
                return;
            }

            // 2. Same-site requests only (stops other websites from spending your quota)
            if (!IsSameSite(context))
            {
                Send(context, false);
                return;
            }

            // 3. Read and validate the body
            string message = ""; string page = "";
            List<ChatTurn> history = new List<ChatTurn>();
            try
            {
                string body;
                using (StreamReader reader = new StreamReader(context.Request.InputStream))
                {
                    body = reader.ReadToEnd();
                }
                if (body.Length > 10000)
                {
                    Send(context, false);
                    return;
                }

                JObject obj = JObject.Parse(body);
                message = ((string)obj["message"] ?? "").Trim();
                page = ((string)obj["page"] ?? "").Trim();
                if (page.Length > 80 || !System.Text.RegularExpressions.Regex.IsMatch(page, @"^[A-Za-z0-9/_\-\.]*$")) page = "";

                JArray items = obj["history"] as JArray;
                if (items != null)
                {
                    foreach (JToken item in items)
                    {
                        string role = ((string)item["role"] ?? "").Trim();
                        string text = ((string)item["text"] ?? "").Trim();
                        if ((role != "user" && role != "model") || text.Length == 0) continue;
                        if (text.Length > MaxTurnLength) text = text.Substring(0, MaxTurnLength);
                        history.Add(new ChatTurn { Role = role, Text = text });
                    }
                }
            }
            catch
            {
                Send(context, false);
                return;
            }

            if (message.Length == 0 || message.Length > MaxMessageLength)
            {
                Send(context, false);
                return;
            }

            // Keep only the latest turns, and make sure the history starts with a user turn.
            if (history.Count > MaxHistoryTurns)
            {
                history = history.Skip(history.Count - MaxHistoryTurns).ToList();
            }
            while (history.Count > 0 && history[0].Role != "user")
            {
                history.RemoveAt(0);
            }

            // 4. Rate limits (per visitor address, plus one cap for the whole site per day)
            bool loggedIn = context.User != null && context.User.Identity != null && context.User.Identity.IsAuthenticated;
            string ip = GetClientIp(context);
            if (!Allow("ip:" + ip, loggedIn ? MemberPerTenMinutes : GuestPerTenMinutes, TimeSpan.FromMinutes(10)) ||
                !Allow("day", GlobalPerDay, TimeSpan.FromDays(1)))
            {
                Send(context, false, null, null, "limit");
                return;
            }

            // 5. Ask Gemini. null means "failed" and the page uses the keyword answers.
            string visitor = GetVisitorType(context);
            string reply = AIService.Ask(OakKnowledge.Build(visitor,page), history, message);
            if (string.IsNullOrWhiteSpace(reply))
            {
                Send(context, false);
                return;
            }

            if (reply.Length > 1200) reply = reply.Substring(0, 1200);
            Send(context, true, reply, "happy", null);
        }

        public bool IsReusable
        {
            get { return false; }
        }

        // ---------------------------------------------------------------- helpers

        private static void Send(HttpContext context, bool ok, string reply = null, string mood = null, string reason = null)
        {
            context.Response.Write(JsonConvert.SerializeObject(new { ok, reply, mood, reason }));
        }

        private static bool IsSameSite(HttpContext context)
        {
            string origin = context.Request.Headers["Origin"];
            if (string.IsNullOrEmpty(origin)) return true;   // some same-site requests carry no Origin header
            Uri o;
            return Uri.TryCreate(origin, UriKind.Absolute, out o) &&
                   string.Equals(o.Host, context.Request.Url.Host, StringComparison.OrdinalIgnoreCase);
        }

        // "guest", "client" or "staff", worked out the same way Site.Master.cs does it.
        private static string GetVisitorType(HttpContext context)
        {
            if (context.User == null || context.User.Identity == null || !context.User.Identity.IsAuthenticated) return "guest";

            HttpSessionState s = context.Session;
            if (s != null)
            {
                if (s["ClientID"] != null) return "client";
                foreach (string key in new[] { "UserRole", "Role", "UserType" })
                {
                    if (s[key] != null && s[key].ToString().Equals("Client", StringComparison.OrdinalIgnoreCase)) return "client";
                }
                if (s["UserLevel"] != null || s["StaffName"] != null) return "staff";
            }
            return "client";   // logged in but role unknown: give the least privileged help
        }

        private static string GetClientIp(HttpContext context)
        {
            string ip = context.Request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrEmpty(ip)) ip = ip.Split(',')[0].Trim();
            else ip = context.Request.UserHostAddress ?? "unknown";
            if (ip.Contains(".") && ip.Contains(":")) ip = ip.Substring(0, ip.IndexOf(':'));   // strip ":port" from IPv4
            return ip;
        }

        // Simple counter per time window, kept in the server's memory.
        private static readonly object Gate = new object();

        private static bool Allow(string name, int max, TimeSpan window)
        {
            string key = "chat:" + name + ":" + (DateTime.UtcNow.Ticks / window.Ticks);
            lock (Gate)
            {
                object current = HttpRuntime.Cache[key];
                int n = current is int ? (int)current : 0;
                if (n >= max) return false;
                HttpRuntime.Cache.Insert(key, n + 1, null, DateTime.UtcNow.Add(window).AddMinutes(1),
                    System.Web.Caching.Cache.NoSlidingExpiration);
                return true;
            }
        }
    }
}