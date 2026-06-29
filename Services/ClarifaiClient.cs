using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class ClarifaiClient
{
    private readonly string apiKey;
    private readonly HttpClient client;

    public ClarifaiClient(string apiKey)
    {
        this.apiKey = apiKey;
        this.client = new HttpClient(); // Crea un'istanza di HttpClient
    }

    // Metodo per inviare l'immagine a Clarifai e ottenere la descrizione
    public async Task<ImageAnalysisResult> GetImageDescription(Stream imageStream)
    {
        try
        {
            // Converte lo Stream in un array di byte
            byte[] imageBytes = ReadStream(imageStream);
            string base64Image = Convert.ToBase64String(imageBytes);


            // URL corretto per il modello "General" di Clarifai
            var url = "https://api.clarifai.com/v2/users/greg/apps/MyImageRecognitionApp/workflows/image-description-workflow/results";

          
            var requestBody = new
            {
                inputs = new[]
                {
                    new
                    {
                        data = new
                        {
                            image = new
                            {
                                base64 = base64Image
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            // Crea il contenuto della richiesta con l'immagine

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la richiesta HTTP
            var response = await client.SendAsync(request);

            // Se la richiesta è riuscita, leggi la risposta
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                // return ParseDescriptionFromClarifaiJson(result); // Restituisce il risultato come stringa JSON
                ImageAnalysisResult analysis = ParseDescriptionFromClarifaiJson(result);
                return analysis; // Restituisce l'oggetto ImageAnalysisResult con i tag e i colori
            }
            else
            {
                // Aggiungi il corpo della risposta per ottenere più dettagli sull'errore
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Errore nella chiamata a Clarifai: {response.ReasonPhrase}. Dettagli: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            return new ImageAnalysisResult
            {
                Tags = new List<string> { $"Errore: {ex.Message}" },
                Colors = new List<string>()
            };
        }
    }

    // Funzione di utilità per leggere uno Stream in un array di byte
    private byte[] ReadStream(Stream stream)
    {
        using (var ms = new MemoryStream())
        {
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }

    //serve per estrarre la descrizione dai risultati JSON di Clarifai
    /*
    private string ParseDescriptionFromClarifaiJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            var concepts = doc.RootElement
                .GetProperty("results")[0]
                .GetProperty("outputs")[0]
                .GetProperty("data")
                .GetProperty("concepts");

            var tags = concepts
                .EnumerateArray()
                .Select(c => c.GetProperty("name").GetString())
                .ToList();

            if (tags.Count == 0)
                return "Nessuna descrizione disponibile.";

            var description = $"L'immagine contiene: {string.Join(", ", tags)}.";
            return description;
        }
        catch
        {
            return "Errore nell'interpretazione della risposta Clarifai.";
        }
    }*/
    public ImageAnalysisResult ParseDescriptionFromClarifaiJson(string json)
    {
        var result = new ImageAnalysisResult();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var outputs = root.GetProperty("results")[0].GetProperty("outputs");

        foreach (var output in outputs.EnumerateArray())
        {
            var modelId = output.GetProperty("model").GetProperty("id").GetString();

            // Estrai TAG dal visual-classifier
            if (modelId == "general-image-recognition" || modelId == "visual-classifier")
            {
                if (output.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("concepts", out var concepts))
                {
                    foreach (var concept in concepts.EnumerateArray())
                    {
                        var name = concept.GetProperty("name").GetString();
                        result.Tags.Add(name);
                    }
                }
            }

            // Estrai COLORI dal color-recognition
            if (modelId == "color-recognition" || modelId == "image-color-recognizer")
            {
                if (output.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("colors", out var colors))
                {
                    foreach (var color in colors.EnumerateArray())
                    {
                        var name = color.GetProperty("w3c").GetProperty("name").GetString();
                        result.Colors.Add(name);
                    }
                }
            }
        }

        return result;
    }

    // Metodo per estrarre i tag dai risultati JSON di Clarifai
    public List<string> GetTagsFromClarifaiJson(string json)
    {
        var tags = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                return tags;

            var output = results[0].GetProperty("outputs")[0];
            var data = output.GetProperty("data");

            if (!data.TryGetProperty("concepts", out var concepts))
                return tags;

            tags = concepts
                .EnumerateArray()
                .Select(c => c.GetProperty("name").GetString())
                .ToList();
        }
        catch (Exception ex)
        {
            // logga o ignora
        }

        return tags;
    }


    // Metodo per ottenere la risposta grezza da Clarifai, utile per debugging o analisi avanzata
    public async Task<string> GetClarifaiRawResponse(Stream imageStream)
    {
        try
        {
            byte[] imageBytes = ReadStream(imageStream);
            string base64Image = Convert.ToBase64String(imageBytes);
            
            var url = "https://api.clarifai.com/v2/users/greg/apps/MyImageRecognitionApp/workflows/image-description-workflow/results";

            var requestBody = new
            {
                inputs = new[]
                {
                new
                {
                    data = new
                    {
                        image = new
                        {
                            base64 = base64Image
                        }
                    }
                }
            }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Errore Clarifai: {response.StatusCode}. {responseString}");

            return responseString;
        }
        catch (Exception ex)
        {
            return $"{{ \"errore\": \"{ex.Message}\" }}";
        }
    }




    // Metodo per rilevare ombre utilizzando `general-image-recognition` e `color-recognition`
    public async Task<bool> DetectShadowsAsync(Stream imageStream, double shadowThreshold = 0.5)
    {
        try
        {
            // Converte lo Stream in un array di byte
            byte[] imageBytes = ReadStream(imageStream);
            string base64Image = Convert.ToBase64String(imageBytes);
            
            // URL per il modello "General" di Clarifai
            var url = "https://api.clarifai.com/v2/users/greg/apps/MyImageRecognitionApp/workflows/image-description-workflow/results";

            var requestBody = new
            {
                inputs = new[]
                {
                new
                {
                    data = new
                    {
                        image = new
                        {
                            base64 = base64Image
                        }
                    }
                }
            }
            };

            var json = JsonSerializer.Serialize(requestBody);
            // Crea la richiesta
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Key", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // Invia la richiesta HTTP
            var response = await client.SendAsync(request);
            // Se la richiesta è riuscita, leggi la risposta
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var analysis = ParseDescriptionFromClarifaiJson(result);

                // Analizza i concetti restituiti dai modelli per cercare ombre
                bool tagDetected = false;
                bool colorDetected = false;
                bool shadowDetected = false;

                // Controlla se tra le etichette c'è qualcosa che indica ombre
                foreach (var tag in analysis.Tags)
                {
                    if (tag.ToLower().Contains("shadow") || tag.ToLower().Contains("dark") || tag.ToLower().Contains("blur"))
                    {
                        Console.WriteLine($"Ombra rilevata: {tag}");
                        tagDetected = true;
                    }
                }

                // Analizza i colori per trovare tonalità scure
                foreach (var color in analysis.Colors)
                {
                    if (color.ToLower().Contains("black") || color.ToLower().Contains("gray"))
                    {
                        Console.WriteLine($"Colore scuro trovato: {color}");
                        colorDetected = true;
                    }
                }

                shadowDetected = tagDetected && colorDetected;
                return shadowDetected;
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                throw new Exception($"Errore nella chiamata a Clarifai: {response.ReasonPhrase}. Dettagli: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore durante il rilevamento delle ombre: {ex.Message}");
            return false;
        }
    }



}
