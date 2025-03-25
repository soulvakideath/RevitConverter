using Autodesk.Revit.DB;
using RevitConverter.Core.Enums;
using RevitConverter.Core.Interfaces;
using RevitConverter.Core.Models;
using RevitConverter.Utilities;

namespace RevitConverter.Conventers
{
    public class BrepConverter : IGeometryConverter
    {
        private readonly IProgressReporter _progressReporter;

        public BrepConverter(IProgressReporter progressReporter = null)
        {
            _progressReporter = progressReporter ?? new ProgressReporter();
        }

        public async Task<ConversionResult> ConvertAsync(
            GeometryModel model, 
            ConversionOptions options, 
            IProgressReporter progressReporter, 
            CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                progressReporter?.ReportError("Geometry model is null.");
                return ConversionResult.InvalidInput;
            }

            if (model.GeometryType != GeometryType.Brep)
            {
                progressReporter?.ReportError($"Expected BRep geometry but got {model.GeometryType}.");
                return ConversionResult.UnsupportedGeometry;
            }

            if (model.GeometryData == null)
            {
                progressReporter?.ReportError("Geometry data is null.");
                return ConversionResult.InvalidInput;
            }

            try
            {
                progressReporter?.ReportStatus("Starting BRep conversion...", 0);

                // Отримати документ Revit
                var doc = options.RevitDocument as Document;
                if (doc == null)
                {
                    progressReporter?.ReportError("Invalid Revit document.");
                    return ConversionResult.RevitError;
                }

                // Аналіз типу BRep геометрії
                progressReporter?.ReportStatus("Analyzing BRep geometry...", 20);
                
                // Визначити тип BRep об'єкта (solid, surface, curve)
                var result = ConversionResult.Failed;
                
                if (model.GeometryData is Solid solid)
                {
                    progressReporter?.ReportStatus("Converting Solid BRep...", 40);
                    var convertedSolid = ConvertSolid(solid, options, doc);
                    if (convertedSolid != null)
                    {
                        model.RevitObject = convertedSolid;
                        result = ConversionResult.Success;
                    }
                }
                else if (model.GeometryData is Face face)
                {
                    progressReporter?.ReportStatus("Converting Surface BRep...", 40);
                    var convertedSurface = ConvertSurface(face, options, doc);
                    if (convertedSurface != null)
                    {
                        model.RevitObject = convertedSurface;
                        result = ConversionResult.Success;
                    }
                }
                else if (model.GeometryData is Curve curve)
                {
                    progressReporter?.ReportStatus("Converting Curve BRep...", 40);
                    var convertedCurve = ConvertCurve(curve, options, doc);
                    if (convertedCurve != null)
                    {
                        model.RevitObject = convertedCurve;
                        result = ConversionResult.Success;
                    }
                }
                else
                {
                    progressReporter?.ReportError("Unsupported BRep geometry type.");
                    return ConversionResult.UnsupportedGeometry;
                }

                if (result == ConversionResult.Success)
                {
                    progressReporter?.ReportStatus("BRep conversion completed successfully.", 100);
                    progressReporter?.ReportCompletion("BRep conversion completed.", true);
                }
                else
                {
                    progressReporter?.ReportError("Failed to convert BRep geometry.");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                progressReporter?.ReportStatus("BRep conversion cancelled.", 0);
                return ConversionResult.Cancelled;
            }
            catch (Exception ex)
            {
                progressReporter?.ReportError($"Error during BRep conversion: {ex.Message}", ex);
                return ConversionResult.Failed;
            }
        }

        public bool CanConvert(GeometryType geometryType)
        {
            return geometryType == GeometryType.Brep;
        }

        private Element ConvertSolid(Solid solid, ConversionOptions options, Document doc)
        {
            if (solid == null || doc == null)
                return null;
            
            try
            {
                using (var transaction = new Transaction(doc, "Convert Solid BRep"))
                {
                    transaction.Start();
                    
                    // Створення DirectShape для представлення BRep solid
                    var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                    
                    // Використовуємо відповідну категорію з моделі, якщо вказана
                    ElementId elementCategoryId = categoryId;
                    if (options.ParameterMapping.TryGetValue("Category", out string categoryName))
                    {
                        Category category = Category.GetCategory(doc, categoryName);
                        if (category != null)
                            elementCategoryId = category.Id;
                    }
                    
                    // Створюємо DirectShape
                    var directShape = DirectShape.CreateElement(doc, elementCategoryId);
                    
                    if (directShape != null)
                    {
                        // Встановлюємо геометрію
                        var geometryObjects = new List<GeometryObject> { solid };
                        directShape.SetShape(geometryObjects);
                        
                        // Встановлюємо ім'я та інші параметри
                        if (!string.IsNullOrEmpty(options.TemplateFilePath))
                        {
                            var param = directShape.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                            if (param != null && !param.IsReadOnly)
                            {
                                param.Set(Path.GetFileNameWithoutExtension(options.TemplateFilePath));
                            }
                        }
                        
                        transaction.Commit();
                        return directShape;
                    }
                    else
                    {
                        transaction.RollBack();
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Element ConvertSurface(Face face, ConversionOptions options, Document doc)
        {
            if (face == null || doc == null)
                return null;
            
            try
            {
                using (var transaction = new Transaction(doc, "Convert Surface BRep"))
                {
                    transaction.Start();
                    
                    // Отримуємо поверхню
                    var surface = face.GetSurface();
                    
                    // Створюємо форму через DirectShape
                    var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                    var directShape = DirectShape.CreateElement(doc, categoryId);
                    
                    if (directShape != null)
                    {
                        // Створюємо примітивні об'єкти для представлення поверхні
                        var builder = new TessellatedShapeBuilder();
                        builder.OpenConnectedFaceSet(false);
                        
                        // Отримуємо меш поверхні
                        var triangulation = face.Triangulate();
                        var vertices = triangulation.Vertices;
                        var indices = triangulation.GetTriangles();
                        
                        // Створюємо грані з трикутників
                        for (int i = 0; i < indices.Count; i += 3)
                        {
                            var points = new List<XYZ>
                            {
                                vertices[indices[i]],
                                vertices[indices[i + 1]],
                                vertices[indices[i + 2]]
                            };
                            
                            var facet = new TessellatedFace(points, ElementId.InvalidElementId);
                            builder.AddFace(facet);
                        }
                        
                        builder.CloseConnectedFaceSet();
                        builder.Build();
                        var shape = builder.GetBuildResult();
                        
                        // Встановлюємо форму
                        directShape.SetShape(shape.GetGeometricalObjects());
                        
                        transaction.Commit();
                        return directShape;
                    }
                    else
                    {
                        transaction.RollBack();
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Element ConvertCurve(Curve curve, ConversionOptions options, Document doc)
        {
            if (curve == null || doc == null)
                return null;
            
            try
            {
                using (var transaction = new Transaction(doc, "Convert Curve BRep"))
                {
                    transaction.Start();
                    
                    // Створюємо ModelCurve
                    Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, curve.GetEndPoint(0));
                    SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
                    
                    var modelCurve = doc.Create.NewModelCurve(curve, sketchPlane);
                    
                    transaction.Commit();
                    return modelCurve;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}