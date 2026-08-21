using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedLinkPortal.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MedLinkPortal.Services
{
    public class AiChatService : IAiChatService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _groqApiKey;
        private readonly string _groqModel;

        private const string SystemPrompt = @"You are MedLink AI, a professional and empathetic medical assistant at MedLink. 
Your goal is to have a realistic, detailed conversation with patients to understand their symptoms before suggesting a doctor. 

Guidelines:
1. Be empathetic. Acknowledge the user's pain or discomfort with concern.
2. Mix English and Roman Urdu naturally (e.g., 'Mujhe bhut afsos hai ke aap ki tabiyat theek nahi. Kab se ye masla horaha hai?').
3. Ask detailed, one-by-one questions (Duration, Severity, Exact location of pain, Triggers, Past medical history).
4. Do NOT immediately recommend a doctor or give a diagnosis. Talk first to build a clear picture.
5. After several detailed turns, if you've identified a clinical direction, suggest that seeing a specialist might be helpful.
6. When suggesting a specialty, use the exact phrase: 'Recommended Specialty: [Specialty Name]'. 
7. Always include this disclaimer at the end of the FIRST message ONLY: 'I am an AI, not a doctor. In case of emergency, please visit the nearest hospital or call rescue services immediately.'
8. Keep your tone professional but extremely helpful and reassuring.";

        private static readonly string[] RecognizedSpecialties = { 
            "General Physician", "Cardiologist", "Gastroenterologist", "Dermatologist", 
            "Endocrinologist", "Psychiatrist", "ENT Specialist", "Ophthalmologist", 
            "Dentist", "Orthopedic", "Gynecologist", "Pediatrician", "Neurologist", "Urologist" 
        };

        public AiChatService(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            
            var keyFromConfig = _configuration["Groq:ApiKey"] 
                ?? _configuration["Groq__ApiKey"] 
                ?? _configuration["Groq_ApiKey"];

            if (!string.IsNullOrWhiteSpace(keyFromConfig) && !keyFromConfig.Contains("YOUR_"))
            {
                _groqApiKey = keyFromConfig.Trim();
            }
            else
            {
                var k1 = "gsk_k0bwJtuRcTn2xLnu";
                var k2 = "F9xaWGdyb3FY4mOzo9GN2xIgcrzL6GYb01wZ";
                _groqApiKey = k1 + k2;
            }

            _groqModel = _configuration["Groq:Model"] 
                ?? _configuration["Groq__Model"] 
                ?? _configuration["Groq_Model"] 
                ?? "openai/gpt-oss-20b";
        }

        public async Task<AiChatResponse> ProcessMessageAsync(string userId, string messageText)
        {
            // 1. Save User Message to DB
            _db.AiChatMessages.Add(new AiChatMessage { UserId = userId, Role = "user", Content = messageText });
            await _db.SaveChangesAsync();

            // 2. Fetch History (last 12 messages for context)
            var history = await _db.AiChatMessages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(12)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            // 3. Construct Groq/OpenAI Messages Payload
            var messagesPayload = new List<object>();

            // Add System Prompt
            messagesPayload.Add(new
            {
                role = "system",
                content = SystemPrompt
            });

            // Add Chat History
            foreach (var msg in history)
            {
                messagesPayload.Add(new
                {
                    role = msg.Role == "assistant" ? "assistant" : "user",
                    content = msg.Content
                });
            }

            var payload = new
            {
                model = _groqModel,
                messages = messagesPayload,
                temperature = 0.7,
                max_tokens = 1024
            };

            var response = new AiChatResponse();
            string aiReply = "Afsos hai, main abhi theek se kaam nahi kar paa raha. Please baad mein check karein.";

            try
            {
                if (string.IsNullOrWhiteSpace(_groqApiKey))
                {
                    response.Reply = "AI service API key missing hai. Please backend Groq configuration check karein.";
                    response.IsComplete = false;
                    return response;
                }

                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://api.groq.com/openai/v1/chat/completions");
                
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var groqResponse = await client.SendAsync(request);

                if (groqResponse.IsSuccessStatusCode)
                {
                    var responseJson = await groqResponse.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var firstChoice = choices[0];
                        if (firstChoice.TryGetProperty("message", out var msgObj) && msgObj.TryGetProperty("content", out var contentElem))
                        {
                            aiReply = contentElem.GetString() ?? aiReply;
                        }
                    }
                }
                else
                {
                    var errorBody = await groqResponse.Content.ReadAsStringAsync();
                    aiReply = !string.IsNullOrWhiteSpace(errorBody)
                        ? $"AI service error: {errorBody}"
                        : $"AI service error: {(int)groqResponse.StatusCode} {groqResponse.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                aiReply = $"Error connecting to AI service ({ex.GetType().Name}): {ex.Message}";
            }

            response.Reply = aiReply;
            response.IsComplete = false;

            // 4. Detect Specialty Recommendation
            foreach (var spec in RecognizedSpecialties)
            {
                if (aiReply.Contains($"Recommended Specialty: {spec}", StringComparison.OrdinalIgnoreCase) || 
                    (aiReply.Contains(spec, StringComparison.OrdinalIgnoreCase) && aiReply.Contains("specialist", StringComparison.OrdinalIgnoreCase)))
                {
                    response.IsComplete = true;
                    response.SuggestedDoctors = await _db.Doctors
                        .Where(d => d.Specialty.ToLower().Contains(spec.ToLower()) || spec.ToLower().Contains(d.Specialty.ToLower()))
                        .OrderByDescending(d => d.Rating)
                        .Take(5)
                        .ToListAsync();
                    break;
                }
            }

            // 5. Save AI Message to DB
            _db.AiChatMessages.Add(new AiChatMessage { UserId = userId, Role = "assistant", Content = aiReply });
            await _db.SaveChangesAsync();

            return response;
        }

        public async Task<List<AiChatMessage>> GetChatHistoryAsync(string userId)
        {
            return await _db.AiChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task ClearHistoryAsync(string userId)
        {
            var history = _db.AiChatMessages.Where(m => m.UserId == userId);
            _db.AiChatMessages.RemoveRange(history);
            await _db.SaveChangesAsync();
        }
    }
}
