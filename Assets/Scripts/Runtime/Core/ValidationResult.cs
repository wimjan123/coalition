using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coalition.Runtime.Core
{
    /// <summary>
    /// Represents the result of a data validation operation, including errors and warnings.
    /// </summary>
    public class ValidationResult
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();

        /// <summary>
        /// True if any validation errors were found.
        /// </summary>
        public bool HasErrors => errors.Count > 0;

        /// <summary>
        /// True if any validation warnings were found.
        /// </summary>
        public bool HasWarnings => warnings.Count > 0;

        /// <summary>
        /// True if validation passed without errors or warnings.
        /// </summary>
        public bool IsValid => !HasErrors && !HasWarnings;

        /// <summary>
        /// Number of validation errors found.
        /// </summary>
        public int ErrorCount => errors.Count;

        /// <summary>
        /// Number of validation warnings found.
        /// </summary>
        public int WarningCount => warnings.Count;

        /// <summary>
        /// All validation errors.
        /// </summary>
        public IReadOnlyList<string> Errors => errors;

        /// <summary>
        /// All validation warnings.
        /// </summary>
        public IReadOnlyList<string> Warnings => warnings;

        /// <summary>
        /// Add a validation error.
        /// </summary>
        /// <param name="error">Error message</param>
        public void AddError(string error)
        {
            if (!string.IsNullOrEmpty(error))
                errors.Add(error);
        }

        /// <summary>
        /// Add a validation warning.
        /// </summary>
        /// <param name="warning">Warning message</param>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrEmpty(warning))
                warnings.Add(warning);
        }

        /// <summary>
        /// Merge another validation result into this one.
        /// </summary>
        /// <param name="other">Other validation result</param>
        /// <param name="prefix">Optional prefix for messages</param>
        public void Merge(ValidationResult other, string prefix = null)
        {
            if (other == null) return;

            foreach (var error in other.errors)
            {
                AddError(string.IsNullOrEmpty(prefix) ? error : $"{prefix}: {error}");
            }

            foreach (var warning in other.warnings)
            {
                AddWarning(string.IsNullOrEmpty(prefix) ? warning : $"{prefix}: {warning}");
            }
        }

        /// <summary>
        /// Get a summary of all errors.
        /// </summary>
        /// <returns>Formatted error summary</returns>
        public string GetErrorSummary()
        {
            if (!HasErrors) return "No errors";

            var sb = new StringBuilder();
            sb.AppendLine($"Validation Errors ({ErrorCount}):");
            for (int i = 0; i < errors.Count; i++)
            {
                sb.AppendLine($"  {i + 1}. {errors[i]}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get a summary of all warnings.
        /// </summary>
        /// <returns>Formatted warning summary</returns>
        public string GetWarningSummary()
        {
            if (!HasWarnings) return "No warnings";

            var sb = new StringBuilder();
            sb.AppendLine($"Validation Warnings ({WarningCount}):");
            for (int i = 0; i < warnings.Count; i++)
            {
                sb.AppendLine($"  {i + 1}. {warnings[i]}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get a complete summary of errors and warnings.
        /// </summary>
        /// <returns>Formatted complete summary</returns>
        public string GetFullSummary()
        {
            var sb = new StringBuilder();

            if (HasErrors)
            {
                sb.AppendLine(GetErrorSummary());
            }

            if (HasWarnings)
            {
                if (HasErrors) sb.AppendLine();
                sb.AppendLine(GetWarningSummary());
            }

            if (!HasErrors && !HasWarnings)
            {
                sb.AppendLine("Validation passed - no errors or warnings");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clear all errors and warnings.
        /// </summary>
        public void Clear()
        {
            errors.Clear();
            warnings.Clear();
        }

        /// <summary>
        /// Create a new validation result with a single error.
        /// </summary>
        /// <param name="error">Error message</param>
        /// <returns>Validation result with the error</returns>
        public static ValidationResult WithError(string error)
        {
            var result = new ValidationResult();
            result.AddError(error);
            return result;
        }

        /// <summary>
        /// Create a new validation result with a single warning.
        /// </summary>
        /// <param name="warning">Warning message</param>
        /// <returns>Validation result with the warning</returns>
        public static ValidationResult WithWarning(string warning)
        {
            var result = new ValidationResult();
            result.AddWarning(warning);
            return result;
        }

        /// <summary>
        /// Create a successful validation result.
        /// </summary>
        /// <returns>Validation result with no errors or warnings</returns>
        public static ValidationResult Success()
        {
            return new ValidationResult();
        }

        public override string ToString()
        {
            return GetFullSummary();
        }
    }
}