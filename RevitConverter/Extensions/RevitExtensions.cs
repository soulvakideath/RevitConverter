using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;

namespace RevitConverter.Extensions
{
    /// <summary>
    /// Extension methods for Revit-related operations.
    /// Contains:
    /// - ToDirectShape: Converts a geometry model to a Revit DirectShape.
    /// - ToFamilyInstance: Converts a geometry model to a Revit FamilyInstance.
    /// - ToRevitElements: Converts a collection of geometry models to Revit elements.
    /// - GetRevitElementType: Gets the appropriate Revit element type for a geometry model.
    /// - SaveAsRfa: Saves a family object to a .rfa file.
    /// - SaveAsRvt: Saves a document object to a .rvt file.
    /// </summary>
    public static class RevitExtensions
    {
        /// <summary>
        /// Converts a geometry model to a Revit DirectShape.
        /// </summary>
        /// <param name="model">The geometry model to convert.</param>
        /// <param name="document">The Revit document.</param>
        /// <returns>The created Revit DirectShape object.</returns>
        public static object ToDirectShape(this GeometryModel model, object document)
        {
            if (model == null || model.GeometryData == null || document == null)
            {
                return null;
            }

            // This would be implemented with actual Revit API code
            // to create a DirectShape from the geometry model
            
            // For now, return null as a placeholder
            return null;
        }

        /// <summary>
        /// Converts a geometry model to a Revit FamilyInstance.
        /// </summary>
        /// <param name="model">The geometry model to convert.</param>
        /// <param name="document">The Revit document.</param>
        /// <param name="familySymbol">The Revit family symbol to use.</param>
        /// <returns>The created Revit FamilyInstance object.</returns>
        public static object ToFamilyInstance(this GeometryModel model, object document, object familySymbol)
        {
            if (model == null || document == null || familySymbol == null)
            {
                return null;
            }

            // This would be implemented with actual Revit API code
            // to create a FamilyInstance from the geometry model
            
            // For now, return null as a placeholder
            return null;
        }

        /// <summary>
        /// Converts a collection of geometry models to Revit elements.
        /// </summary>
        /// <param name="models">The geometry models to convert.</param>
        /// <param name="document">The Revit document.</param>
        /// <returns>A collection of created Revit element objects.</returns>
        public static IEnumerable<object> ToRevitElements(this IEnumerable<GeometryModel> models, object document)
        {
            if (models == null || document == null)
            {
                yield break;
            }

            foreach (var model in models)
            {
                // Create the appropriate type of element based on the model's properties
                object element = null;
                
                switch (model.ElementType)
                {
                    case RevitElementType.Wall:
                    case RevitElementType.Floor:
                    case RevitElementType.Ceiling:
                    case RevitElementType.Roof:
                        // Create a specific element type if possible
                        break;
                    
                    default:
                        // Default to DirectShape for generic geometry
                        element = model.ToDirectShape(document);
                        break;
                }
                
                if (element != null)
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// Gets the appropriate Revit element type for a geometry model.
        /// </summary>
        /// <param name="model">The geometry model.</param>
        /// <returns>The determined Revit element type.</returns>
        public static RevitElementType GetRevitElementType(this GeometryModel model)
        {
            if (model == null)
            {
                return RevitElementType.GenericModel;
            }

            // If already specified, use that
            if (model.ElementType != RevitElementType.GenericModel)
            {
                return model.ElementType;
            }

            // This would be implemented with actual geometry analysis logic
            // to determine the appropriate Revit element type
            
            // For now, use the name as a hint
            string name = model.Name?.ToLower() ?? string.Empty;
            
            if (name.Contains("wall"))
            {
                return RevitElementType.Wall;
            }
            else if (name.Contains("floor"))
            {
                return RevitElementType.Floor;
            }
            else if (name.Contains("ceiling"))
            {
                return RevitElementType.Ceiling;
            }
            else if (name.Contains("roof"))
            {
                return RevitElementType.Roof;
            }
            else if (name.Contains("column"))
            {
                return RevitElementType.Column;
            }
            else if (name.Contains("beam"))
            {
                return RevitElementType.Beam;
            }
            else if (name.Contains("door"))
            {
                return RevitElementType.Door;
            }
            else if (name.Contains("window"))
            {
                return RevitElementType.Window;
            }
            else if (name.Contains("furniture"))
            {
                return RevitElementType.Furniture;
            }
            
            return RevitElementType.GenericModel;
        }

        /// <summary>
        /// Saves a family object to a .rfa file.
        /// </summary>
        /// <param name="family">The family object to save.</param>
        /// <param name="filePath">The file path to save to.</param>
        /// <param name="overwrite">Whether to overwrite the file if it exists.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool SaveAsRfa(this RevitFamily family, string filePath, bool overwrite = false)
        {
            if (family == null || string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            // Check if file exists and should not be overwritten
            if (File.Exists(filePath) && !overwrite)
            {
                return false;
            }

            try
            {
                // This would be implemented with actual Revit API code
                // to save the family to a .rfa file
                
                // For now, return true as a placeholder
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Saves a document object to a .rvt file.
        /// </summary>
        /// <param name="document">The document object to save.</param>
        /// <param name="filePath">The file path to save to.</param>
        /// <param name="overwrite">Whether to overwrite the file if it exists.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool SaveAsRvt(this object document, string filePath, bool overwrite = false)
        {
            if (document == null || string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            // Check if file exists and should not be overwritten
            if (File.Exists(filePath) && !overwrite)
            {
                return false;
            }

            try
            {
                // This would be implemented with actual Revit API code
                // to save the document to a .rvt file
                
                // For now, return true as a placeholder
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}