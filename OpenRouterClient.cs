using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;

namespace code
{
    public class OpenRouterClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint = "https://openrouter.ai/api/v1/chat/completions";
        private readonly string _model;

        public OpenRouterClient(string model = "openrouter/auto")
        {
            _httpClient = new HttpClient();
            _apiKey = LoadApiKey();
            _model = model;
        }

        private string LoadApiKey()
        {
            var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            if (!File.Exists(envPath))
                throw new Exception(".env file not found");
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (line.StartsWith("OPENROUTER_API_KEY="))
                    return line.Substring("OPENROUTER_API_KEY=".Length).Trim();
            }
            throw new Exception("OPENROUTER_API_KEY not found in .env");
        }

        public async Task<string> AskAsync(string systemPrompt, string userPrompt)
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 256,
                temperature = 0.2
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            string raw = string.Empty;
            try
            {
                var response = await _httpClient.SendAsync(request);
                raw = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return $"OpenRouter API error: {response.StatusCode} - {raw}";
                }
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var contentProp))
                        return contentProp.GetString() ?? "";
                }
                return "No answer from OpenRouter API.";
            }
            catch (Exception ex)
            {
                return $"OpenRouter API call failed: {ex.Message}\nRaw response: {raw}";
            }
        }
    }
}
