using System.Security.Cryptography;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageDescriptionApp.Model.Requests;
using ImageDescriptionApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly ClarifaiClient _clarifaiClient;
        private readonly ComputerVisionService _computerVisionService;
        private readonly GroqService _groqService;
        private readonly QualityCheck _qualityCheck;

        public ImageController(
            ComputerVisionService computerVisionService,
            GroqService groqService,
            ClarifaiClient clarifaiClient,
            QualityCheck qualityCheck)
        {
            _computerVisionService = computerVisionService;
            _groqService = groqService;
            _clarifaiClient = clarifaiClient;
            _qualityCheck = qualityCheck;
        }

        // Unico endpoint supportato dal frontend
        [HttpPost("describe-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> DescribeImage([FromForm] ImageUploadRequest request)
        {
            var image = request.Image;

            if (image == null || image.Length == 0)
                return BadRequest(new { message = "Nessun file immagine caricato." });

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/bmp", "image/webp" };
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };

            var contentType = image.ContentType.ToLower();
            var extension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedMimeTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Formato immagine non supportato. Sono accettati solo i formati: JPG, JPEG, PNG, BMP, WEBP."
                });
            }

            if (request.UserMessage != null && string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest(new { message = "Il messaggio non può contenere solo spazi vuoti. Inserisci del testo significativo." });

            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var image2 = LoadImageFromBytes(imageBytes);

                if (!_qualityCheck.IsQualitySufficient(image2, out List<string> unsatisfiedCriteria,
                                                       brightnessThreshold: 50,
                                                       contrastThreshold: 10,
                                                       minWidth: 600,
                                                       minHeight: 400,
                                                       maxWidth: 1920,
                                                       maxHeight: 1080))
                {
                    return BadRequest(new
                    {
                        message = "Immagine non ha una qualità sufficiente per essere elaborata. Inserirne un'altra.",
                        criteriNonSoddisfatti = unsatisfiedCriteria
                    });
                }

                memoryStream.Position = 0;
                bool hasShadows = await _clarifaiClient.DetectShadowsAsync(memoryStream);

                if (hasShadows)
                {
                    return BadRequest(new { message = "L'immagine contiene ombre. Reinserire un'immagine senza ombre!" });
                }

                memoryStream.Position = 0;
                var clarifaiRawJson = await _clarifaiClient.GetClarifaiRawResponse(memoryStream);
                var analysisResult = _clarifaiClient.ParseDescriptionFromClarifaiJson(clarifaiRawJson);

                memoryStream.Position = 0;
                var basicDescription = await _computerVisionService.GetImageDescriptionAsync(memoryStream);

                var groqEnhancedDescription = await _groqService.GenerateDescriptionFromTags(
                    analysisResult,
                    basicDescription,
                    request.UserMessage ?? ""
                );

                var hash = ComputeSha256Hash(imageBytes);

                return Ok(new
                {
                    AzureDescription = basicDescription,
                    ClarifaiTags = analysisResult.Tags,
                    ClarifaiColors = analysisResult.Colors,
                    UserMessage = request.UserMessage,
                    GroqEnhancedDescription = groqEnhancedDescription,
                    ImageHash = hash
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante l'elaborazione dell'immagine.", error = ex.Message });
            }
        }



        private Image<Bgr, byte> LoadImageFromBytes(byte[] imageBytes)
        {
            Mat matImage = new Mat();
            CvInvoke.Imdecode(imageBytes, ImreadModes.Color, matImage);
            return matImage.ToImage<Bgr, byte>();
        }

        public static string ComputeSha256Hash(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
