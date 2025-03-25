using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;

namespace RevitConverter.Extensions
{
    /// <summary>
    /// Extension methods for geometry-related operations.
    /// Contains:
    /// - ToRevitCoordinates: Converts coordinates to Revit's coordinate system.
    /// - GetBoundingBox: Gets the bounding box of a geometry model.
    /// - Simplify: Simplifies a geometry model to reduce complexity.
    /// - MergeGeometryModels: Merges multiple geometry models into one.
    /// - ToMesh: Converts a geometry model to a mesh representation.
    /// - Clone: Creates a deep clone of a geometry model.
    /// </summary>
    public static class GeometryExtensions
    {
        /// <summary>
        /// Converts coordinates to Revit's coordinate system.
        /// </summary>
        /// <param name="coordinates">The coordinates to convert.</param>
        /// <returns>The converted coordinates.</returns>
        public static double[] ToRevitCoordinates(this double[] coordinates)
        {
            if (coordinates == null || coordinates.Length < 3)
            {
                return new double[] { 0, 0, 0 };
            }

            // Revit uses a right-handed coordinate system with Z up
            // This example assumes input is in a right-handed system with Y up
            // and converts it to Revit's system
            return new double[]
            {
                coordinates[0],
                coordinates[2],
                coordinates[1]
            };
        }

        /// <summary>
        /// Gets the bounding box of a geometry model.
        /// </summary>
        /// <param name="model">The geometry model.</param>
        /// <returns>The bounding box as min and max points.</returns>
        public static (double[] Min, double[] Max) GetBoundingBox(this GeometryModel model)
        {
            if (model == null || model.GeometryData == null)
            {
                return (new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 });
            }

            // This would be implemented with actual geometry processing logic
            // based on the type of geometry data

            // For now, return placeholder bounding box
            return (new double[] { -100, -100, -100 }, new double[] { 100, 100, 100 });
        }

        /// <summary>
        /// Simplifies a geometry model to reduce complexity.
        /// </summary>
        /// <param name="model">The geometry model to simplify.</param>
        /// <param name="tolerance">The simplification tolerance.</param>
        /// <returns>The simplified geometry model.</returns>
        public static GeometryModel Simplify(this GeometryModel model, double tolerance)
        {
            if (model == null || model.GeometryData == null)
            {
                return model;
            }

            // Create a new model for the simplified geometry
            var simplified = model.Clone();

            // Simplify based on geometry type
            switch (model.GeometryType)
            {
                case GeometryType.Mesh:
                    // This would be implemented with actual mesh simplification logic
                    break;

                case GeometryType.Brep:
                    // This would be implemented with actual BRep simplification logic
                    break;
            }

            return simplified;
        }

        /// <summary>
        /// Merges multiple geometry models into one.
        /// </summary>
        /// <param name="models">The geometry models to merge.</param>
        /// <returns>The merged geometry model, or null if no models to merge.</returns>
        public static GeometryModel MergeGeometryModels(this IEnumerable<GeometryModel> models)
        {
            if (models == null || !models.Any())
            {
                return null;
            }

            if (models.Count() == 1)
            {
                return models.First().Clone();
            }

            // Create a new model for the merged geometry
            var merged = new GeometryModel
            {
                Name = "Merged Model",
                GeometryType = models.First().GeometryType
            };

            // For now, just add the models as children
            foreach (var model in models)
            {
                merged.Children.Add(model.Clone());
            }

            // In a real implementation, this would merge the actual geometry data

            return merged;
        }

        /// <summary>
        /// Converts a geometry model to a mesh representation.
        /// </summary>
        /// <param name="model">The geometry model to convert.</param>
        /// <param name="tolerance">The meshing tolerance.</param>
        /// <returns>The mesh representation of the model.</returns>
        public static GeometryModel ToMesh(this GeometryModel model, double tolerance)
        {
            if (model == null || model.GeometryData == null)
            {
                return model;
            }

            // If already a mesh, just return a clone
            if (model.GeometryType == GeometryType.Mesh)
            {
                return model.Clone();
            }

            // Create a new model for the mesh
            var mesh = model.Clone();
            mesh.GeometryType = GeometryType.Mesh;

            // Convert BRep to mesh
            if (model.GeometryType == GeometryType.Brep)
            {
                // This would be implemented with actual BRep to mesh conversion logic
                // mesh.GeometryData = ConvertBrepToMesh(model.GeometryData, tolerance);
            }

            return mesh;
        }

        /// <summary>
        /// Creates a deep clone of a geometry model.
        /// </summary>
        /// <param name="model">The geometry model to clone.</param>
        /// <returns>A deep clone of the model.</returns>
        public static GeometryModel Clone(this GeometryModel model)
        {
            if (model == null)
            {
                return null;
            }

            var clone = new GeometryModel
            {
                Id = model.Id,
                Name = model.Name,
                GeometryType = model.GeometryType,
                ElementType = model.ElementType,
                GeometryData = model.GeometryData // Note: In a real implementation, this would create a deep clone of the geometry data
            };

            // Clone parameters
            foreach (var param in model.Parameters)
            {
                clone.Parameters[param.Key] = param.Value;
            }

            // Clone children recursively
            foreach (var child in model.Children)
            {
                clone.Children.Add(child.Clone());
            }

            return clone;
        }
    }
}