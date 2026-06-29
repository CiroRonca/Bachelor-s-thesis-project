using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ImageDescriptionApp.Entities
{
    [Table("Images")]
    public class Image
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public byte[] ImageData { get; set; }

        public string? UserMessage { get; set; }
        public string? GroqDescription { get; set; }
        public string? AzureDescription { get; set; }
        public string? ClarifaiTags { get; set; }
        public string? ClarifaiColors { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? ContentType { get; set; }


        [Required]
        public string ImageHash { get; set; }
    }
}
