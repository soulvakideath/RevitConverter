using System;
using System.Collections.Generic;
using RevitConverter.Core.Enums;

namespace RevitConverter.Core.Models
{
    public class GeometryModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Unnamed Model";
        public GeometryType GeometryType { get; set; } = GeometryType.Unknown;
        public RevitElementType ElementType { get; set; } = RevitElementType.GenericModel;
        public object GeometryData { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public GeometryModel Parent { get; set; }
        public List<GeometryModel> Children { get; set; } = new List<GeometryModel>();
        public bool IsValid => GeometryData != null && GeometryType != GeometryType.Unknown;
        
        public object RevitObject { get; set; }
    }
}