using System;
using System.Collections.Generic;

namespace RevitConverter.Core.Models
{
    /// <summary>
    /// Represents a Revit family with its parameters and type catalog.
    /// Contains:
    /// - Id: Unique identifier for the family.
    /// - Name: Name of the Revit family.
    /// - Category: Category of the Revit family.
    /// - TemplateFile: Template for creating the family.
    /// - Parameters: Parameters of the family.
    /// - TypeParameters: Type parameters for the type catalog.
    /// - GeometryModels: Associated geometry models that make up this family.
    /// - FilePath: Path to the created family file.
    /// - IsValid: Indicates if the family has valid parameters required for creation.
    /// </summary>
    public class RevitFamily
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Category { get; set; }
        public string TemplateFile { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<Dictionary<string, object>> TypeParameters { get; set; } = new List<Dictionary<string, object>>();
        public List<GeometryModel> GeometryModels { get; set; } = new List<GeometryModel>();
        public string FilePath { get; set; }
        public bool IsValid => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Category);
    }
}