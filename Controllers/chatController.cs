using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace mediguideBack.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class chatController : ControllerBase
    {
        private readonly string azureApiKey = "******";
        private readonly string azureEndpoint = "****";
        private readonly string deploymentName = "*******"; 

        [HttpPost("getresponse")]
        public async Task<IActionResult> GetChatResponse([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Prompt))
            {
                return BadRequest("Prompt cannot be empty.");
            }

            try
            {
                var requestBody = new
                {
                    model = deploymentName,
                    messages = new[]
                    {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = request.Prompt }
                },
                    temperature = 0.7,
                    max_tokens = 5000
                };

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("api-key", azureApiKey);

                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{azureEndpoint}/openai/deployments/{deploymentName}/chat/completions?api-version=2024-02-01")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
                    };

                    var response = await client.SendAsync(requestMessage);

                    if (response.IsSuccessStatusCode)
                    {
                        var resultJson = await response.Content.ReadAsStringAsync();
                        var resultObj = JsonSerializer.Deserialize<JsonElement>(resultJson);
                        string cleanResponse = resultObj.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "Aucune réponse disponible.";

                        return Ok(cleanResponse);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        return StatusCode(500, $"Azure OpenAI API error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class ChatRequest
    {
        public string Prompt { get; set; }
    }

}

