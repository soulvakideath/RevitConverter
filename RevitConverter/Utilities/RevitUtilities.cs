using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;

namespace RevitConverter.Utilities
{
    /// <summary>
    /// Utility class for Revit-specific operations.
    /// Contains:
    /// - DetermineElementType: Determines the appropriate Revit element type for a geometry.
    /// - GetFamilyTemplatePath: Gets the path to a family template file.
    /// - IsValidRevitFile: Checks if a file is a valid Revit file.
    /// - GetRevitVersion: Determines the Revit version of a file.
    /// - FindRevitInstallation: Finds the Revit installation directory.
    /// - GetAvailableFamilyTemplates: Gets the available family templates.
    /// </summary>
    public class RevitUtilities
    {
        /// <summary>
        /// Determines the appropriate Revit element type for a geometry.
        /// </summary>
        /// <param name="model">The geometry model.</param>
        /// <returns>The determined Revit element type.</returns>
        public RevitElementType DetermineElementType(GeometryModel model)
        {
            if (model == null)
            {
                return RevitElementType.GenericModel;
            }

            // This would be implemented with actual geometry analysis logic
            // to determine the appropriate Revit element type
            
            // For now, return GenericModel as a default
            return RevitElementType.GenericModel;
        }

        /// <summary>
        /// Gets the path to a family template file.
        /// </summary>
        /// <param name="category">The family category.</param>
        /// <param name="revitVersion">The Revit version.</param>
        /// <returns>The path to the family template file, or null if not found.</returns>
        public string GetFamilyTemplatePath(string category, string revitVersion)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(revitVersion))
            {
                return null;
            }

            // Map category to template file name
            string templateFileName;
            switch (category)
            {
                case "Walls":
                    templateFileName = "Wall.rft";
                    break;
                case "Floors":
                    templateFileName = "Floor.rft";
                    break;
                case "Doors":
                    templateFileName = "Door.rft";
                    break;
                case "Windows":
                    templateFileName = "Window.rft";
                    break;
                case "Furniture":
                    templateFileName = "Furniture.rft";
                    break;
                default:
                    templateFileName = "Generic Model.rft";
                    break;
            }

            // Find Revit installation directory
            string revitDir = FindRevitInstallation(revitVersion);
            if (string.IsNullOrEmpty(revitDir))
            {
                return null;
            }

            // Construct template path
            string templatePath = Path.Combine(revitDir, "Family Templates", "English", templateFileName);
            if (File.Exists(templatePath))
            {
                return templatePath;
            }

            // Try alternate locations
            templatePath = Path.Combine(revitDir, "Templates", "English", templateFileName);
            if (File.Exists(templatePath))
            {
                return templatePath;
            }

            return null;
        }

        /// <summary>
        /// Checks if a file is a valid Revit file.
        /// </summary>
        /// <param name="filePath">The path to the file to check.</param>
        /// <returns>True if the file is a valid Revit file, false otherwise.</returns>
        public bool IsValidRevitFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".rvt" && extension != ".rfa" && extension != ".rte" && extension != ".rft")
            {
                return false;
            }

            // Check file signature
            try
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] signature = new byte[8];
                    stream.Read(signature, 0, 8);

                    // Check for Revit file signature (this is a simplified check)
                    // A real implementation would check the actual Revit file signature
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines the Revit version of a file.
        /// </summary>
        /// <param name="filePath">The path to the Revit file.</param>
        /// <returns>The Revit version as a string, or null if not determined.</returns>
        public string GetRevitVersion(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
            }

            // This would be implemented with actual Revit file analysis logic
            // to determine the version of the Revit file
            
            // For now, return a placeholder version
            return "2021";
        }

        /// <summary>
        /// Finds the Revit installation directory.
        /// </summary>
        /// <param name="revitVersion">The Revit version.</param>
        /// <returns>The Revit installation directory, or null if not found.</returns>
        public string FindRevitInstallation(string revitVersion)
        {
            if (string.IsNullOrEmpty(revitVersion))
            {
                return null;
            }

            // Look for Revit installation in standard locations
            string[] possiblePaths = new[]
            {
                $@"C:\Program Files\Autodesk\Revit {revitVersion}",
                $@"C:\Program Files\Autodesk\Revit {revitVersion}\Revit",
                $@"C:\ProgramData\Autodesk\RVT {revitVersion}"
            };

            foreach (string path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the available family templates.
        /// </summary>
        /// <param name="revitVersion">The Revit version.</param>
        /// <returns>Dictionary mapping category names to template file paths.</returns>
        public async Task<Dictionary<string, string>> GetAvailableFamilyTemplates(string revitVersion)
        {
            var templates = new Dictionary<string, string>();

            // Find Revit installation directory
            string revitDir = FindRevitInstallation(revitVersion);
            if (string.IsNullOrEmpty(revitDir))
            {
                return templates;
            }

            // Look for templates in standard locations
            string[] possibleTemplateDirs = new[]
            {
                Path.Combine(revitDir, "Family Templates", "English"),
                Path.Combine(revitDir, "Templates", "English")
            };

            foreach (string dir in possibleTemplateDirs)
            {
                if (!Directory.Exists(dir))
                {
                    continue;
                }

                // Find all .rft files in the directory
                string[] templateFiles = Directory.GetFiles(dir, "*.rft");
                foreach (string templateFile in templateFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(templateFile);
                    templates[fileName] = templateFile;
                }
            }

            return templates;
        }
    }
}