using System.Collections.Generic;

namespace RevitConverter.Core.Models
{
    public class ConversionOptions
    {
        public string RevitVersion { get; set; } = "2021";
        public bool CreateFamily { get; set; } = true;
        public string OutputFilePath { get; set; }
        public double Tolerance { get; set; } = 0.001;
        public bool MergeCoincidentVertices { get; set; } = true;
        public bool IncludeHiddenGeometry { get; set; } = false;
        public Dictionary<string, string> ParameterMapping { get; set; } = new Dictionary<string, string>();
        public string TemplateFilePath { get; set; }
        public bool DetailedProgress { get; set; } = false;
        public bool AutoDetermineFamilyType { get; set; } = true;
    }
}