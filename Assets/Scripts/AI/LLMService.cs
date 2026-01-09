using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ChessBattle.Core;

namespace ChessBattle.AI
{
    public enum LLMProvider { OpenAI, Grok }

    public interface IChessAI
    {
        Task<string> GetMoveAsync(string fen, List<string> legalMoves, string personality, LLMProvider provider);
    }

    [Serializable]
    public class OpenAIRequest
    {
        public string model;
        public List<OpenUIMessage> messages;
        public float temperature;
    }

    [Serializable]
    public class OpenUIMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class OpenAIResponse
    {
        public List<Choice> choices;
    }

    [Serializable]
    public class Choice
    {
        public OpenUIMessage message;
    }

    public class LLMService : MonoBehaviour, IChessAI
    {
        [Header("API Configuration")]
        public string OpenAI_ApiKey = "YOUR_KEY";
        public string Grok_ApiKey = "YOUR_KEY";
        
        public string OpenAI_Model = "gpt-4o-mini"; 
        public string Grok_Model = "grok-beta";
        
        public string OpenAI_Endpoint = "https://api.openai.com/v1/chat/completions";
        public string Grok_Endpoint = "https://api.x.ai/v1/chat/completions";
        
        [Header("Debug")]
        public bool DebugResponses = true;

        [Serializable]
        private class ApiKeysConfig
        {
            public string OpenAI_Key;
            public string Grok_Key;
        }

        private void Awake()
        {
            LoadApiKeys();
        }

        private void LoadApiKeys()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Secrets/ApiKeys.json");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    ApiKeysConfig config = JsonUtility.FromJson<ApiKeysConfig>(json);
                    
                    if (!string.IsNullOrEmpty(config.OpenAI_Key)) OpenAI_ApiKey = config.OpenAI_Key;
                    if (!string.IsNullOrEmpty(config.Grok_Key)) Grok_ApiKey = config.Grok_Key;
                    
                    Debug.Log("[LLMService] Loaded API Keys.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LLMService] Failed to load API Key: {e.Message}");
                }
            }
        }

        public async Task<string> GetMoveAsync(string fen, List<string> legalMoves, string personality, LLMProvider provider)
        {
            string apiKey = (provider == LLMProvider.OpenAI) ? OpenAI_ApiKey : Grok_ApiKey;
            string endpoint = (provider == LLMProvider.OpenAI) ? OpenAI_Endpoint : Grok_Endpoint;
            string model = (provider == LLMProvider.OpenAI) ? OpenAI_Model : Grok_Model;

            if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR_KEY"))
            {
                Debug.LogError($"{provider} API Key is missing!");
                return "";
            }

            // Construct JSON
            string jsonBody = ConstructPayload(fen, legalMoves, personality, model);
            
            // Web Request
            using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                var operation = request.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"{provider} API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    return "";
                }

                string responseText = request.downloadHandler.text;
                if (DebugResponses) Debug.Log($"[{provider}] Response: {responseText}");

                // Parse (Response format is identical for OpenAI and Grok/xAI)
                return ParseResponse(responseText, legalMoves);
            }
        }

        private string ConstructPayload(string fen, List<string> legalMoves, string personality, string modelName)
        {
            string systemPrompt = $"You are a chess engine. Personality: {personality}. " +
                                  $"Current FEN: {fen}. " +
                                  $"List of legal moves: {string.Join(", ", legalMoves)}. " +
                                  $"Choose the best move from the list for the current turn. " +
                                  $"IMPORTANT: If promoting a pawn, you MUST select a move with the promotion suffix (e.g., 'e7e8q' for Queen, 'e7e8r' for Rook). " +
                                  $"Reply ONLY with the exact move string (e.g. 'e2e4' or 'a7a8q'). Do not add any reasoning or punctuation.";

            OpenAIRequest req = new OpenAIRequest
            {
                model = modelName,
                temperature = 0.7f,
                messages = new List<OpenUIMessage>
                {
                    new OpenUIMessage { role = "system", content = "You are a helpful chess assistant." },
                    new OpenUIMessage { role = "user", content = systemPrompt }
                }
            };

            return JsonUtility.ToJson(req);
        }

        private string ParseResponse(string json, List<string> legalMoves)
        {
            try
            {
                OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(json);
                if (response != null && response.choices != null && response.choices.Count > 0)
                {
                    string content = response.choices[0].message.content.Trim();
                    // Cleanup (remove quotes, periods, whitespace)
                    content = content.Replace("\"", "").Replace("'", "").Replace(".", "").Trim();
                    
                    // Simple validation
                    if (legalMoves.Contains(content)) return content;
                    
                    // Fuzzy match
                    foreach(var move in legalMoves)
                    {
                        if (content.Contains(move)) return move;
                    }
                     Debug.LogWarning($"LLM suggested illegal move: {content}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing LLM response: {e.Message}");
            }
            return "";
        }
    }
}
