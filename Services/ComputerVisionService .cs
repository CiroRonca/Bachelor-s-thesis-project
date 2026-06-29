using System.Net.Http.Headers;
using Newtonsoft.Json;

public class ComputerVisionService
{
    private readonly string _apiKey;
    private readonly string _endpoint;

    public ComputerVisionService(string apiKey, string endpoint)
    {
        _apiKey = apiKey;
        _endpoint = endpoint;
    }

    public async Task<string> GetImageDescriptionAsync(Stream imageStream)
    {
        var requestUri = $"{_endpoint}/vision/v3.2/analyze?visualFeatures=Description&language=en";

        imageStream.Position = 0;

        using var client = new HttpClient();
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = content
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

        var response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Errore Azure: {response.StatusCode} - {json}");

        dynamic result = JsonConvert.DeserializeObject(json);
        return result?.description?.captions?[0]?.text ?? "Nessuna descrizione trovata.";
    }

}
