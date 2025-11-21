using System.ComponentModel.DataAnnotations;

namespace SecureCleanApiWaf.Models
{
    /// <summary>
    /// Represents a sample Data Transfer Object (DTO) used in the Blazor Server application's API layer for model binding and validation.
    /// This class demonstrates best practices for input validation using data annotations, ensuring data integrity before processing
    /// in controllers or services. It aligns with the project's API design principles, including model validation via attributes
    /// like [Required], which enforces mandatory fields to prevent invalid data from external sources, as outlined in the README.
    /// 
    /// <para>
    /// <b>Model Validation Guidance:</b>
    /// <list type="bullet">
    /// <item>Each model should be properly validated to ensure data integrity and security.</item>
    /// <item>Use data annotation attributes such as [Required], [StringLength], [Range], [EmailAddress], etc. to enforce validation rules.</item>
    /// <item>Validate models in controllers using ModelState.IsValid before processing requests.</item>
    /// <item>For complex validation, implement IValidatableObject or use custom validation attributes.</item>
    /// <item>Always return meaningful error messages for invalid data to help clients correct their input.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class SampleDtoModel
    {
        [Required]
        public string Name { get; set; }
    }
}
