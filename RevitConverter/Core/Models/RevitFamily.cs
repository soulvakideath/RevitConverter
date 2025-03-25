using System;
using System.Collections.Generic;

namespace RevitConverter.Core.Models
{
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
        public object RevitDocument { get; set; }
    }
}