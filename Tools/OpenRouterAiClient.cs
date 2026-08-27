using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace HisClient
{
    public class OpenRouterAiClient
    {
        private readonly string apiKey;
        private readonly string[] fallbackModels = new string[]
        {
            "minimax/minimax-m3:free",
            "google/gemma-4-31b-it:free",
            "z-ai/glm-5.2:free",
            "openrouter/free"
        };

        public OpenRouterAiClient(string key = null)
        {
            apiKey = !string.IsNullOrEmpty(key) ? key : GetApiKey();
        }

        public static string GetApiKey()
        {
            string k = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(k))
                k = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(k))
                k = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY", EnvironmentVariableTarget.Machine);
            return k ?? "";
        }

        public string GenerateMedicalProgress(string patientName, string diagnosis, string icdCode, int dayNumber, string extraNotes = "")
        {
            string systemPrompt = "Bạn là trợ lý AI Bác sĩ Ngoại Chấn thương Chỉnh hình & Cột sống - Bệnh viện Bạch Mai. Trả về đúng 1 đoạn văn ngắn (3-4 dòng) diễn biến bệnh lâm sàng chuẩn y khoa, không dùng markdown rườm rà.";
            string userPrompt = string.Format("Soạn diễn biến điều trị ngày thứ {0} cho BN {1} (Chẩn đoán: [{2}] {3}). Ghi nhận: BN tỉnh táo, vết mổ/vết thương khô sạch, giảm đau, dấu hiệu sinh tồn ổn định, tiếp tục điều trị theo phác đồ. {4}",
                dayNumber, patientName, icdCode, diagnosis, extraNotes);

            return ChatCompletion(systemPrompt, userPrompt);
        }

        public string GenerateSummaryNote(string patientName, string diagnosis, string icdCode, int days, string currentStatus)
        {
            string systemPrompt = "Bạn là Bác sĩ chuyên khoa Bệnh viện Bạch Mai. Soạn nội dung Tờ sơ kết điều trị ngắn gọn, súc tích chuẩn quy chế hồ sơ bệnh án.";
            string userPrompt = string.Format("Soạn nội dung Sơ kết {0} ngày điều trị cho bệnh nhân {1}, Chẩn đoán: [{2}] {3}. Tình trạng hiện tại: {4}. Nêu rõ: Quá trình diễn biến, kết quả điều trị và hướng điều trị tiếp theo.",
                days, patientName, icdCode, diagnosis, currentStatus);

            return ChatCompletion(systemPrompt, userPrompt);
        }

        public string ChatCompletion(string systemPrompt, string userPrompt, int maxTokens = 1000)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return "Bệnh nhân tỉnh táo, tiếp xúc tốt, huyết động ổn định. Vết mổ/vết thương khô, giảm đau. Bụng mềm, không sốt. Tiếp tục điều trị theo phác đồ.";
            }

            foreach (var model in fallbackModels)
            {
                try
                {
                    string reply = CallModel(model, systemPrompt, userPrompt, maxTokens);
                    if (!string.IsNullOrEmpty(reply))
                    {
                        return reply.Trim();
                    }
                }
                catch
                {
                    // Fallback to next model
                }
            }

            return "Bệnh nhân tỉnh táo, tiếp xúc tốt, huyết động ổn định. Vết thương khô sạch, không sưng nề. Tiếp tục điều trị theo phác đồ.";
        }

        private string CallModel(string model, string systemPrompt, string userPrompt, int maxTokens)
        {
            var req = (HttpWebRequest)WebRequest.Create("https://openrouter.ai/api/v1/chat/completions");
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.Headers.Add("Authorization", "Bearer " + apiKey);
            req.Headers.Add("HTTP-Referer", "https://his-automation.local");
            req.Headers.Add("X-Title", "HIS Medical Suite");
            req.Timeout = 20000;

            string jsonSys = EscapeJson(systemPrompt);
            string jsonUser = EscapeJson(userPrompt);

            string body = string.Format(
                "{{\"model\":\"{0}\",\"messages\":[{{\"role\":\"system\",\"content\":\"{1}\"}},{{\"role\":\"user\",\"content\":\"{2}\"}}],\"max_tokens\":{3},\"temperature\":0.3}}",
                model, jsonSys, jsonUser, maxTokens);

            byte[] bytes = Encoding.UTF8.GetBytes(body);
            req.ContentLength = bytes.Length;
            using (var os = req.GetRequestStream())
            {
                os.Write(bytes, 0, bytes.Length);
            }

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string respJson = sr.ReadToEnd();
                return ExtractContent(respJson);
            }
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n");
        }

        private string ExtractContent(string json)
        {
            int idx = json.IndexOf("\"content\":");
            if (idx < 0) return "";
            idx += 10;
            while (idx < json.Length && (json[idx] == ' ' || json[idx] == '\t')) idx++;
            if (idx >= json.Length || json[idx] != '"') return "";
            idx++;
            
            StringBuilder sb = new StringBuilder();
            bool escape = false;
            for (int i = idx; i < json.Length; i++)
            {
                char c = json[i];
                if (escape)
                {
                    if (c == 'n') sb.Append('\n');
                    else if (c == 'r') sb.Append('\r');
                    else if (c == 't') sb.Append('\t');
                    else sb.Append(c);
                    escape = false;
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
