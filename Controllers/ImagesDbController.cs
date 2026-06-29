using System.Security.Cryptography;
using Emgu.CV;
using Emgu.CV.Structure;
using ImageDescriptionApp.Data;
using ImageDescriptionApp.Entities;
using ImageDescriptionApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageDescriptionApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesDbController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GroqService _groqService;
        private readonly ComputerVisionService _computerVisionService;
        private readonly ClarifaiClient _clarifaiClient;

        public ImagesDbController(AppDbContext context,
                                  GroqService groqService,
                                  ComputerVisionService computerVisionService,
                                  ClarifaiClient clarifaiClient)
        {
            _context = context;
            _groqService = groqService;
            _computerVisionService = computerVisionService;
            _clarifaiClient = clarifaiClient;
        }

        // GET: api/imagesdb
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Image>>> GetImages()
        {
            return await _context.Images.ToListAsync();
        }

        // GET: api/imagesdb/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Image>> GetImage(int id)
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null) return NotFound();
            return image;
        }

        // POST: api/imagesdb/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Image>> UploadImage([FromForm] IFormFile image, [FromForm] string? userMessage = null)
        {
            if (image == null || image.Length == 0)
                return BadRequest("Nessuna immagine caricata.");

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/bmp", "image/webp" };
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            var contentType = image.ContentType.ToLower();
            var extension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedMimeTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
            {
                return BadRequest("Formato immagine non supportato. Sono accettati solo: JPG, JPEG, PNG, BMP, WEBP.");
            }

            if (userMessage != null && string.IsNullOrWhiteSpace(userMessage))
                return BadRequest("Il messaggio non può contenere solo spazi vuoti.");

            byte[] imageData;
            using (var ms = new MemoryStream())
            {
                await image.CopyToAsync(ms);
                imageData = ms.ToArray();
            }

            string hash;
            using (var sha = SHA256.Create())
            {
                hash = Convert.ToBase64String(sha.ComputeHash(imageData));
            }

            if (_context.Images.Any(i => i.ImageHash == hash))
                return BadRequest("Immagine già presente nel database.");

            var qc = new QualityCheck();
            var imgForCheck = LoadImageFromBytes(imageData);

            if (!qc.IsQualitySufficient(imgForCheck, out List<string> unsatisfiedCriteria))
            {
                return BadRequest(new
                {
                    message = "Immagine non ha una qualità sufficiente per essere elaborata. Inserirne un'altra.",
                    criteriNonSoddisfatti = unsatisfiedCriteria
                });
            }

            string groqDescription = "";
            string azureDescription = "";
            string clarifaiTags = "";
            string clarifaiColors = "";

            try
            {
                ImageAnalysisResult clarifaiResult = new ImageAnalysisResult();

                if (_computerVisionService != null)
                {
                    using var msAzure = new MemoryStream(imageData);
                    azureDescription = await _computerVisionService.GetImageDescriptionAsync(msAzure);
                }

                if (_clarifaiClient != null)
                {
                    using var msClarifai = new MemoryStream(imageData);
                    clarifaiResult = await _clarifaiClient.GetImageDescription(msClarifai);
                    clarifaiTags = clarifaiResult.Tags != null ? string.Join(",", clarifaiResult.Tags) : "";
                    clarifaiColors = clarifaiResult.Colors != null ? string.Join(",", clarifaiResult.Colors) : "";
                }

                if (_groqService != null)
                {
                    groqDescription = await _groqService.GenerateDescriptionFromTags(clarifaiResult, azureDescription, userMessage ?? "");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante analisi API.", error = ex.Message });
            }

            var newImage = new Image
            {
                FileName = image.FileName,
                ImageData = imageData,
                ContentType = image.ContentType,
                UserMessage = userMessage,
                GroqDescription = groqDescription,
                AzureDescription = azureDescription,
                ClarifaiTags = clarifaiTags,
                ClarifaiColors = clarifaiColors,
                ImageHash = hash
            };

            _context.Images.Add(newImage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetImage), new { id = newImage.Id }, newImage);
        }

        private Image<Bgr, byte> LoadImageFromBytes(byte[] imageBytes)
        {
            var mat = new Emgu.CV.Mat();
            Emgu.CV.CvInvoke.Imdecode(imageBytes, Emgu.CV.CvEnum.ImreadModes.Color, mat);
            return mat.ToImage<Emgu.CV.Structure.Bgr, byte>();
        }

        // PUT per aggiornare Groq (solo dal servizio Groq)
        [HttpPut("{id}/updateGroq")]
        public async Task<IActionResult> UpdateGroqDescription(int id, [FromBody] string newGroq)
        {
            var existing = await _context.Images.FindAsync(id);
            if (existing == null) return NotFound();

            existing.GroqDescription = newGroq;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existing).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT per aggiornare il messaggio utente
        [HttpPut("{id}/updateMessage")]
        public async Task<IActionResult> UpdateUserMessage(int id, [FromBody] UpdateDescriptionRequest request)
        {
            var existing = await _context.Images.FindAsync(id);
            if (existing == null) return NotFound();

            if (string.IsNullOrWhiteSpace(request.UserMessage))
                return BadRequest("Il messaggio non può essere vuoto.");

            try
            {
                string groqDescription = "";
                string azureDescription = "";
                string clarifaiTags = "";
                string clarifaiColors = "";
                ImageAnalysisResult clarifaiResult = new ImageAnalysisResult();

                // Rilancia analisi con le API
                if (_computerVisionService != null)
                {
                    using var msAzure = new MemoryStream(existing.ImageData);
                    azureDescription = await _computerVisionService.GetImageDescriptionAsync(msAzure);
                }

                if (_clarifaiClient != null)
                {
                    using var msClarifai = new MemoryStream(existing.ImageData);
                    clarifaiResult = await _clarifaiClient.GetImageDescription(msClarifai);
                    clarifaiTags = clarifaiResult.Tags != null ? string.Join(",", clarifaiResult.Tags) : "";
                    clarifaiColors = clarifaiResult.Colors != null ? string.Join(",", clarifaiResult.Colors) : "";
                }

                if (_groqService != null)
                {
                    groqDescription = await _groqService.GenerateDescriptionFromTags(
                        clarifaiResult,
                        azureDescription,
                        request.UserMessage ?? ""
                    );
                }

                // Aggiorna i campi
                existing.UserMessage = request.UserMessage;
                existing.GroqDescription = groqDescription;
                existing.AzureDescription = azureDescription;
                existing.ClarifaiTags = clarifaiTags;
                existing.ClarifaiColors = clarifaiColors;
                existing.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(existing); // 🔹 così il frontend riceve subito i nuovi valori
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore durante rigenerazione descrizione.", error = ex.Message });
            }
        }


        // DELETE: api/imagesdb/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null) return NotFound();

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET per restituire il file binario
        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetImageFile(int id)
        {
            var image = await _context.Images.FindAsync(id);
            if (image == null) return NotFound();

            return File(image.ImageData, image.ContentType);
        }
    }

    // DTO per aggiornare la descrizione
    public class UpdateDescriptionRequest
    {
        public string UserMessage { get; set; }
    }
}
