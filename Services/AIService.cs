using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
 
namespace GiantGroupM3.Services
{
    /// <summary>One earlier message in the conversation. Role is "user" or "model".</summary>
    public class ChatTurn
    {
        public string Role { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Talks to Google Gemini. Returns the reply text, or null if every model failed.
    /// GeminiModel can hold ONE model or SEVERAL separated by commas, for example:
    ///     gemini-3.1-flash-lite,gemini-3.8-flash
    /// They are tried in order. Free-tier limits are per model, so when the first one is
    /// busy or out of quota, the next one gets a chance. Callers fall back to the keyword answers on null.
    /// The key never leaves the server (secrets.config locally, Azure environment variables live).
    /// </summary>
    public static class AIService
    {
        // One shared HttpClient for the whole app. Each attempt has its own shorter limit (see AttemptSeconds).
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private const int AttemptSeconds = 20;

        static AIService()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public static string Ask(string systemInstruction, IList<ChatTurn> history, string userMessage)
        {
            string key = ConfigurationManager.AppSettings["GeminiApiKey"];
            string modelSetting = ConfigurationManager.AppSettings["GeminiModel"];
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(modelSetting))
            {
                System.Diagnostics.Trace.TraceWarning("Gemini: GeminiApiKey or GeminiModel is not configured.");
                return null;
            }

            string[] models = modelSetting.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string bodyJson = BuildBody(systemInstruction, history, userMessage);

            foreach (string rawModel in models)
            {
                string model = rawModel.Trim();
                if (model.Length == 0) continue;

                string reply = TryModel(key.Trim(), model, bodyJson);
                if (reply != null)
                {
                    System.Diagnostics.Trace.TraceInformation("Gemini: answered by " + model);
                    return reply;
                }
                // otherwise try the next model in the list
            }
            return null;
        }

        private static string BuildBody(string systemInstruction, IList<ChatTurn> history, string userMessage)
        {
            JArray contents = new JArray();
            if (history != null)
            {
                foreach (ChatTurn t in history)
                {
                    contents.Add(MakeTurn(t.Role, t.Text));
                }
            }
            contents.Add(MakeTurn("user", userMessage));

            JObject body = new JObject
            {
                ["systemInstruction"] = new JObject
                {
                    ["parts"] = new JArray(new JObject { ["text"] = systemInstruction })
                },
                ["contents"] = contents,
                ["generationConfig"] = new JObject
                {
                    ["maxOutputTokens"] = 2048,   // thinking models spend part of this budget; raise it if replies come back empty
                    ["temperature"] = 0.3
                }
            };
            return body.ToString(Formatting.None);
        }

        private static string TryModel(string key, string model, string bodyJson)
        {
            try
            {
                string url = "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent";

                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(AttemptSeconds)))
                using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    req.Headers.Add("x-goog-api-key", key);   // key goes in a header, never in the URL
                    req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                    using (HttpResponseMessage res = Http.SendAsync(req, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult())
                    {
                        string json = res.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                        if (!res.IsSuccessStatusCode)
                        {
                            // 400 = bad request body, 401/403 = key problem, 404 = wrong model name, 429 = quota, 503 = busy
                            System.Diagnostics.Trace.TraceWarning("Gemini (" + model + "): HTTP " + (int)res.StatusCode + " " + res.ReasonPhrase);
                            return null;
                        }

                        JObject obj = JObject.Parse(json);
                        JArray parts = obj.SelectToken("candidates[0].content.parts") as JArray;
                        if (parts == null)
                        {
                            string why = (string)obj.SelectToken("candidates[0].finishReason") ?? (string)obj.SelectToken("promptFeedback.blockReason") ?? "no candidates";
                            System.Diagnostics.Trace.TraceWarning("Gemini (" + model + "): no reply text (" + why + ").");
                            return null;
                        }

                        StringBuilder sb = new StringBuilder();
                        foreach (JToken p in parts)
                        {
                            if ((bool?)p["thought"] == true) continue;   // skip "thinking" parts if the model returns them
                            string text = (string)p["text"];
                            if (!string.IsNullOrEmpty(text)) sb.Append(text);
                        }

                        string result = sb.ToString().Trim();
                        return result.Length == 0 ? null : result;
                    }
                }
            }
            catch (Exception ex)
            {
                // Timeouts and network errors end up here. Never log the key or the visitor's message.
                System.Diagnostics.Trace.TraceWarning("Gemini (" + model + "): " + ex.GetType().Name + " - " + ex.Message);
                return null;
            }
        }

        private static JObject MakeTurn(string role, string text)
        {
            return new JObject
            {
                ["role"] = role,
                ["parts"] = new JArray(new JObject { ["text"] = text })
            };
        }
    }
}
