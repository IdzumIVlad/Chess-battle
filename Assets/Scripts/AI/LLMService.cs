using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using ChessBattle.Core;

namespace ChessBattle.AI
{
    public interface IChessAI
    {
        Task<string> GetMoveAsync(string fen, List<string> legalMoves, string personality);
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
        [Header("API Configuration")]
        public string ApiKey = "YOUR_API_KEY_HERE";
        public string Model = "gpt-4o-mini"; // Faster/Cheaper for tests
        public string Endpoint = "https://api.openai.com/v1/chat/completions";
        
        [Header("Debug")]
        public bool DebugResponses = true;

        [Serializable]
        private class ApiKeysConfig
        {
            public string OpenAI_Key;
        }

        private void Awake()
        {
            LoadApiKey();
        }

        private void LoadApiKey()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Secrets/ApiKeys.json");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    ApiKeysConfig config = JsonUtility.FromJson<ApiKeysConfig>(json);
                    if (!string.IsNullOrEmpty(config.OpenAI_Key) && config.OpenAI_Key != "YOUR_KEY_HERE")
                    {
                        ApiKey = config.OpenAI_Key;
                        Debug.Log("[LLMService] Loaded API Key from Secrets/ApiKeys.json");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LLMService] Failed to load API Key: {e.Message}");
                }
            }
        }

        public async Task<string> GetMoveAsync(string fen, List<string> legalMoves, string personality)
        {
            if (string.IsNullOrEmpty(ApiKey) || ApiKey.Contains("YOUR_API"))
            {
                Debug.LogError("OpenAI API Key is missing! Please set it in LLMService.");
                return "";
            }

            // Construct JSON
            string jsonBody = ConstructPayload(fen, legalMoves, personality);
            
            // Web Request
            using (UnityWebRequest request = new UnityWebRequest(Endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + ApiKey);

                var operation = request.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"OpenAI API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    return "";
                }

                string responseText = request.downloadHandler.text;
                if (DebugResponses) Debug.Log($"LLM Response: {responseText}");

                // Parse
                return ParseResponse(responseText, legalMoves);
            }
        }

        private string ConstructPayload(string fen, List<string> legalMoves, string personality)
        {
            string systemPrompt = $"You are a chess engine. Personality: {personality}. " +
                                  $"Current FEN: {fen}. " +
                                  $"List of legal moves: {string.Join(", ", legalMoves)}. " +
                                  $"Choose the best move from the list for the current turn. " +
                                  $"Reply ONLY with the exact move string (e.g. 'e2e4'). Do not add any reasoning or punctuation.";

            OpenAIRequest req = new OpenAIRequest
            {
                model = Model,
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
                    if (legalMoves.Contains(content))
                    {
                        return content;
                    }
                    else
                    {
                         Debug.LogWarning($"LLM suggested illegal move: {content}");
                         
                         // Retry check: maybe it added reasoning? e.g. "My move is e2e4"
                         foreach(var move in legalMoves)
                         {
                             if (content.Contains(move)) return move;
                         }
                    }
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
