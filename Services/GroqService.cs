using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageDescriptionApp.Services
{
    public class GroqService
    {
        private readonly string _apiKey;
        private readonly string _endpoint = "https://api.groq.com/openai/v1/chat/completions";

        private readonly List<string> _userMessagesHistory = new();
        private string? _lastImageHash;

        public GroqService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public void ResetHistory()
        {
            _userMessagesHistory.Clear();
        }

        public void UpdateImageHash(byte[] imageBytes)
        {
            using var sha256 = SHA256.Create();
            var hash = Convert.ToBase64String(sha256.ComputeHash(imageBytes));

            if (_lastImageHash != hash)
            {
                _lastImageHash = hash;
                ResetHistory(); // solo se immagine nuova
            }
        }

        public void AddUserMessageToHistory(string message)
        {
            var trimmed = message.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) &&
                !_userMessagesHistory.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                _userMessagesHistory.Add(trimmed);
            }
        }

        // Metodo per generare una descrizione basata sui tag ottenuti da Clarifai
        public async Task<string> GenerateDescriptionFromTags(ImageAnalysisResult analysis, string basicDescription, string userMessage)
        {
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                AddUserMessageToHistory(userMessage);
            }

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "user", content = BuildPromptForGroq(analysis, basicDescription, _userMessagesHistory) }
                },
                model = "meta-llama/llama-4-scout-17b-16e-instruct"
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Groq raw response: " + responseContent);

            if (!response.IsSuccessStatusCode)
            {
                return $"Errore Groq: {response.StatusCode} - {responseContent}";
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];

                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? "Nessuna descrizione generata da Groq.";
                }
            }

            return "Nessuna descrizione generata da Groq.";
        }

        // Metodo per costruire il prompt da inviare a Groq
        public string BuildPromptForGroq(ImageAnalysisResult analysis, string basicDescription, List<string> userMessages)
        {
            var tags = string.Join(", ", analysis.Tags.Distinct());
            var colors = string.Join(", ", analysis.Colors.Distinct());
            var combinedUserInfo = string.Join(" ", userMessages);
            var hasUserMessage = !string.IsNullOrWhiteSpace(combinedUserInfo);

            var prompt = $@" Hai a disposizione una serie di informazioni per descrivere un'immagine.
- Tag rilevati da Clarifai: {tags}.
- Colori principali individuati: {colors}.
- Descrizione automatica da Azure: {basicDescription}.
{(hasUserMessage ? $"- Informazioni certe dall'utente: {combinedUserInfo}." : "")}

Importante: includi sempre nella descrizione i messaggi inseriti dall'utente. Se l'utente non inserisce nessun messaggio, continua con la descrizione senza dire che l'utente non ha inserito alcun messaggio.
### Istruzioni:

1. Non introdurre mai la descrizione con frasi come “Non avendo informazioni” o “Proverò a descrivere…”. Inizia direttamente con la descrizione visiva, oggettiva, naturale e fluida, come se fosse parte di un catalogo o una guida illustrata.

2. Se l’utente non fornisce alcuna informazione aggiuntiva, descrivi ciò che si vede in modo diretto, senza elenchi, senza citare colori o tag letteralmente (es. “Tan”, “Gray”, ecc.).
Usa i tag e i colori solo per ispirare la descrizione, non per riportarli testualmente.

3. Cerca di dedurre di che colore sono gli elementi presenti nell'immagine sulla base delle informazioni che hai a disposizione.

4. Evita di aggiungere aggettivi inutili ai fini descrittivi, come 'quest'affascinante abitazione' o 'questo elegante mobile'. Concentrati su ciò che è visibile e rilevante per l'immagine.
Scrivi una descrizione elegante, raffinata e fluida (in stile da catalogo).

- Se l’utente ha fornito solo alcune informazioni (es. solo il titolo , il nome dell'autore o solo le dimensioni), cerca comunque di dedurre il resto se possibile, oppure indica solo ciò che è certo.
Esempio:
Dimensioni: 80x120 cm

5. Concludi sempre con:
La descrizione fornita è esaustiva? Se così non fosse, scrivimelo pure in un messaggio.

Scrivi tutto in italiano, senza ripetizioni né linguaggio artificiale."
;
            return prompt.Trim();
        }
    }
}
