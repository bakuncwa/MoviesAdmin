using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MoviesAdmin.Validation
{
    // Validates an uploaded poster (IFormFile) by extension and size. Runs on the server like any
    // ValidationAttribute, and also emits data-val-posterfile-* attributes that wwwroot/js/validation.js
    // checks in the browser, so a wrong file type or an oversized file is flagged before upload.
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PosterFileAttribute : ValidationAttribute, IClientModelValidator
    {
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        public PosterFileAttribute()
        {
            ErrorMessage = "Poster image must be a .jpg, .jpeg, .png, .gif, or .webp file of at most 5 MB.";
        }

        public override bool IsValid(object? value)
        {
            // Optional field: no file chosen is valid; [Required] would be the place to demand one.
            if (value is not IFormFile file)
            {
                return true;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension) && file.Length > 0 && file.Length <= MaxBytes;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            var displayName = context.ModelMetadata.GetDisplayName();
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-posterfile", FormatErrorMessage(displayName));
            context.Attributes.TryAdd("data-val-posterfile-extensions", string.Join(",", AllowedExtensions));
            context.Attributes.TryAdd("data-val-posterfile-maxbytes", MaxBytes.ToString(CultureInfo.InvariantCulture));
        }
    }
}
