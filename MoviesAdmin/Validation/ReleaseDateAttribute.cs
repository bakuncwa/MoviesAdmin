using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MoviesAdmin.Validation
{
    // Keeps release dates plausible: no earlier than the first surviving motion picture (Roundhay
    // Garden Scene, 14 Oct 1888) and no more than a few years out, for announced films. [Range] can't
    // express this, since its bounds must be constants and jQuery Validate compares them as numbers.
    // Emits data-val-releasedate-* for the matching check in wwwroot/js/validation.js.
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ReleaseDateAttribute : ValidationAttribute, IClientModelValidator
    {
        public static readonly DateTime Earliest = new(1888, 10, 14);
        public const int MaxYearsAhead = 5;

        public static DateTime Latest => DateTime.Today.AddYears(MaxYearsAhead);

        public ReleaseDateAttribute()
        {
            ErrorMessage = "{0} must be between 14 Oct 1888 and {1:d MMM yyyy}.";
        }

        public override string FormatErrorMessage(string name) =>
            string.Format(ErrorMessageString, name, Latest);

        public override bool IsValid(object? value) =>
            value is not DateTime date || (date.Date >= Earliest && date.Date <= Latest);

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-releasedate", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
            context.Attributes.TryAdd("data-val-releasedate-min", Earliest.ToString("yyyy-MM-dd"));
            context.Attributes.TryAdd("data-val-releasedate-max", Latest.ToString("yyyy-MM-dd"));
        }
    }
}
