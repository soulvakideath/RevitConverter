using RevitConverter.Core.Models;

namespace RevitConverter.Conventers
{
    public class ParameterConverter
    {
        private readonly Dictionary<string, string> _parameterMapping;

        public ParameterConverter(Dictionary<string, string> parameterMapping = null)
        {
            _parameterMapping = parameterMapping ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Converts a dictionary of source parameters to Revit parameters.
        /// </summary>
        /// <param name="sourceParameters">Source parameters to convert.</param>
        /// <param name="options">Conversion options.</param>
        /// <returns>Dictionary of converted parameters suitable for Revit.</returns>
        public Dictionary<string, object> ConvertParameters(
            Dictionary<string, object> sourceParameters,
            ConversionOptions options)
        {
            if (sourceParameters == null)
            {
                return new Dictionary<string, object>();
            }

            var result = new Dictionary<string, object>();

            foreach (var param in sourceParameters)
            {
                string revitParamName = MapParameterName(param.Key, options);
                object revitParamValue = ConvertParameterValue(param.Value);

                if (!string.IsNullOrEmpty(revitParamName) && revitParamValue != null)
                {
                    result[revitParamName] = revitParamValue;
                }
            }

            return result;
        }

        /// <summary>
        /// Maps a source parameter name to a Revit parameter name.
        /// </summary>
        /// <param name="sourceName">Source parameter name.</param>
        /// <param name="options">Conversion options with parameter mapping.</param>
        /// <returns>Mapped Revit parameter name or the original if no mapping exists.</returns>
        private string MapParameterName(string sourceName, ConversionOptions options)
        {
            if (options.ParameterMapping.TryGetValue(sourceName, out string mappedName))
            {
                return mappedName;
            }
            
            if (_parameterMapping.TryGetValue(sourceName, out mappedName))
            {
                return mappedName;
            }

            if (IsValidRevitParameterName(sourceName))
            {
                return sourceName;
            }

            return SanitizeParameterName(sourceName);
        }

        /// <summary>
        /// Converts a parameter value to the appropriate type for Revit.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>Converted value suitable for Revit.</returns>
        private object ConvertParameterValue(object value)
        {
            if (value == null)
            {
                return null;
            }
            if (value is string || value is double || value is int || value is bool)
            {
                return value;
            }
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("yyyy-MM-dd");
            }
            if (value.GetType().IsEnum)
            {
                return value.ToString();
            }

            return value.ToString();
        }

        /// <summary>
        /// Determines the appropriate Revit parameter type for a value.
        /// </summary>
        /// <param name="value">Value to determine type for.</param>
        /// <returns>Revit parameter type as a string.</returns>
        private string GetParameterType(object value)
        {
            if (value == null)
            {
                return "Text";
            }

            if (value is string)
            {
                return "Text";
            }

            if (value is double)
            {
                return "Number";
            }

            if (value is int)
            {
                return "Integer";
            }

            if (value is bool)
            {
                return "YesNo";
            }

            if (value is DateTime)
            {
                return "Text";
            }

            return "Text";
        }

        /// <summary>
        /// Checks if a parameter name is valid for Revit.
        /// </summary>
        /// <param name="name">Name to check.</param>
        /// <returns>True if the name is valid for Revit.</returns>
        private bool IsValidRevitParameterName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // Check for invalid characters in Revit parameter names
            return !name.Contains("<") && 
                   !name.Contains(">") && 
                   !name.Contains("\"") && 
                   !name.Contains("/") && 
                   !name.Contains("\\") && 
                   !name.Contains(":") && 
                   !name.Contains("*") && 
                   !name.Contains("?") && 
                   !name.Contains("|");
        }

        /// <summary>
        /// Sanitizes a parameter name for Revit.
        /// </summary>
        /// <param name="name">Name to sanitize.</param>
        /// <returns>Sanitized name suitable for Revit.</returns>
        private string SanitizeParameterName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Parameter";
            }

            // Replace invalid characters
            return name.Replace("<", "_")
                       .Replace(">", "_")
                       .Replace("\"", "_")
                       .Replace("/", "_")
                       .Replace("\\", "_")
                       .Replace(":", "_")
                       .Replace("*", "_")
                       .Replace("?", "_")
                       .Replace("|", "_");
        }
    }
}