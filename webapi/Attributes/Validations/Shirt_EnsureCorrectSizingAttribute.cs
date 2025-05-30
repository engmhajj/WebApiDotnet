using System.ComponentModel.DataAnnotations;
using webapi.Models;

namespace webapi.Attributes.Validations
{
    public class Shirt_EnsureCorrectSizingAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance is not Shirt shirt)
            {
                return new ValidationResult("Invalid object for shirt size validation.");
            }

            if (string.IsNullOrWhiteSpace(shirt.Gender) || !shirt.Size.HasValue)
            {
                return ValidationResult.Success;
            }

            var size = shirt.Size.Value;

            return shirt.Gender.ToLower() switch
            {
                "men" when size < 8 =>
                    new ValidationResult("For men's shirt, the size should be greater or equal to 8."),
                "women" when size < 6 =>
                    new ValidationResult("For women's shirt, the size should be greater or equal to 6."),
                _ => ValidationResult.Success
            };
        }
    }
}
