using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class ClaudeEditorWindow : EditorWindow
{
    private string userPrompt = "";
    private string responseText = "";
    private Vector2 scrollPosition = Vector2.zero;
    private string apiKey = "";
    private bool isLoading = false;
    
    [MenuItem("Window/Claude AI Assistant")]
    public static void ShowWindow()
    {
        GetWindow<ClaudeEditorWindow>("Claude AI");
    }

    private void OnGUI()
    {
        GUILayout.Label("Claude AI Assistant para Unity", EditorStyles.boldLabel);
        
        GUILayout.Label("API Key de Anthropic:", EditorStyles.label);
        apiKey = EditorGUILayout.PasswordField("API Key:", apiKey);
        
        if (GUILayout.Button("Guardar API Key"))
        {
            EditorPrefs.SetString("ClaudeAPIKey", apiKey);
            EditorUtility.DisplayDialog("Éxito", "API Key guardada correctamente", "OK");
        }
        
        GUILayout.Space(10);
        
        GUILayout.Label("Escribe tu prompt:", EditorStyles.label);
        userPrompt = EditorGUILayout.TextArea(userPrompt, GUILayout.Height(100));
        
        GUILayout.Space(10);
        
        EditorGUI.BeginDisabledGroup(isLoading);
        if (GUILayout.Button("Enviar a Claude", GUILayout.Height(30)))
        {
            string savedKey = EditorPrefs.GetString("ClaudeAPIKey", "");
            if (string.IsNullOrEmpty(savedKey))
            {
                EditorUtility.DisplayDialog("Error", "Debes guardar tu API Key primero", "OK");
            }
            else
            {
                SendPromptToClaude(userPrompt);
            }
        }
        EditorGUI.EndDisabledGroup();
        
        if (isLoading)
        {
            GUILayout.Label("⏳ Esperando respuesta de Claude...", EditorStyles.label);
        }
        
        GUILayout.Space(10);
        GUILayout.Label("Respuesta de Claude:", EditorStyles.boldLabel);
        
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        EditorGUILayout.TextArea(responseText, GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        
        if (GUILayout.Button("Copiar Respuesta"))
        {
            EditorGUIUtility.systemCopyBuffer = responseText;
            EditorUtility.DisplayDialog("Éxito", "Respuesta copiada al portapapeles", "OK");
        }
    }

    private async void SendPromptToClaude(string prompt)
    {
        isLoading = true;
        string apiKey = EditorPrefs.GetString("ClaudeAPIKey", "");
        
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            
            string requestBody = $@"{{
                ""model"": ""claude-3-5-sonnet-20241022"",
                ""max_tokens"": 1024,
                ""messages"": [{{
                    ""role"": ""user"",
                    ""content"": ""{EscapeJson(prompt)}""
                }}]
            }}";

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            
            try
            {
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                if (responseContent.Contains("content"))
                {
                    int startIndex = responseContent.IndexOf("\"text\":\"") + 8;
                    int endIndex = responseContent.IndexOf("\"", startIndex);
                    if (startIndex > 7 && endIndex > startIndex)
                    {
                        responseText = responseContent.Substring(startIndex, endIndex - startIndex);
                        responseText = responseText.Replace("\\n", "\n");
                    }
                }
                else
                {
                    responseText = responseContent;
                }
            }
            catch (System.Exception ex)
            {
                responseText = "Error: " + ex.Message;
            }
        }
        
        isLoading = false;
    }

    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}