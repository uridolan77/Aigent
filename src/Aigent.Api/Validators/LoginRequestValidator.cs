using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aigent.Api.Models;

namespace Aigent.Api.Validators
{
    /// <summary>
    /// Validator for LoginRequest
    /// </summary>
    public class LoginRequestValidator
    {
        /// <summary>
        /// Validates a LoginRequest
        /// </summary>
        /// <param name="request">Request to validate</param>
        /// <returns>Validation results</returns>
        public ValidationResult Validate(LoginRequest request)
        {
            var errors = new List<ValidationError>();
            
            if (string.IsNullOrEmpty(request.Username))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.Username),
                    ErrorMessage = "Username is required"
                });
            }
            
            if (string.IsNullOrEmpty(request.Password))
            {
                errors.Add(new ValidationError
                {
                    PropertyName = nameof(request.Password),
                    ErrorMessage = "Password is required"
                });
            }
            
            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }
    }
}
