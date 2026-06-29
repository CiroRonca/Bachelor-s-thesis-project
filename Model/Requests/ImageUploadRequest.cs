using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace ImageDescriptionApp.Model.Requests
{
    public class ImageUploadRequest
    {
        [FromForm(Name = "image")]
        public IFormFile? Image { get; set; }

        [DefaultValue("")]
        public string? UserMessage { get; set; }
    }

}
