using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Api.Models;

namespace Aigent.Api.Validators
{
    /// <summary>
    /// Validator for CreateAgentRequest
    /// </summary>
    public class CreateAgentRequestValidator
    {
        /// <summary>
        /// Validates a CreateAgentRequest
        /// </summary>
        /// <param name="request">Request to validate</param>
        /// <returns>Validation results</returns>
        public ValidationResult Validate(CreateAgentRequest request)
        {
            var errors = new List<ValidationError>();
            
            if (string.IsNullOrEmpty(request.Name))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.Name),
                    ErrorMessage = "Agent name is required"
                });
            }
            
            // Add additional validation rules as needed
            
            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }
    }
    
    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation succeeded
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
    }
    
    /// <summary>
    /// Validation error
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Name of the property that failed validation
        /// </summary>
        public string PropertyName { get; set; }
        
        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
