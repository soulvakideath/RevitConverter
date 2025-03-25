using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using RevitConverter.Core.Enums;
using RevitConverter.Core.Interfaces;
using RevitConverter.Core.Models;
using Document = Autodesk.Revit.DB.Document;

namespace RevitConverter.Generators
{
    public class FamilyGenerator
    {
        private readonly IProgressReporter _progressReporter;

        public FamilyGenerator(IProgressReporter progressReporter = null)
        {
            _progressReporter = progressReporter;
        }

        public async Task<(RevitFamily Family, ConversionResult Result)> GenerateFamilyAsync(
            IEnumerable<GeometryModel> models,
            string familyName,
            string familyCategory,
            ConversionOptions options,
            IProgressReporter progressReporter,
            CancellationToken cancellationToken = default)
        {
            progressReporter?.ReportStatus("Starting family generation...", 0);

            try
            {
                if (string.IsNullOrEmpty(familyName) || string.IsNullOrEmpty(familyCategory))
                {
                    progressReporter?.ReportError("Family name or category not provided.");
                    return (null, ConversionResult.InvalidInput);
                }

                // Створюємо сімейство
                var family = new RevitFamily
                {
                    Name = familyName,
                    Category = familyCategory,
                    TemplateFile = DetermineFamilyTemplate(familyCategory, options)
                };

                // Додаємо моделі геометрії до сімейства
                if (models != null)
                {
                    foreach (var model in models)
                    {
                        family.GeometryModels.Add(model);
                    }
                }

                progressReporter?.ReportStatus("Creating family document...", 10);
                
                // Отримуємо додаток Revit
                var app = options.RevitApplication as Application;
                if (app == null)
                {
                    progressReporter?.ReportError("Invalid Revit application object.");
                    return (null, ConversionResult.RevitError);
                }
                
                // Створюємо новий документ сімей
                // Створюємо документ сімейства на основі шаблону
                Document famDoc = null;
                try
                {
                    // Перевіряємо, чи існує файл шаблону
                    if (!File.Exists(family.TemplateFile))
                    {
                        progressReporter?.ReportError($"Family template file not found: {family.TemplateFile}");
                        return (null, ConversionResult.InvalidInput);
                    }
                    
                    // Створюємо новий документ сімейства
                    famDoc = app.NewFamilyDocument(family.TemplateFile);
                    family.RevitDocument = famDoc;
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportError($"Failed to create family document: {ex.Message}", ex);
                    return (null, ConversionResult.RevitError);
                }

                progressReporter?.ReportStatus("Creating family parameters...", 30);
                
                // Транзакція для створення параметрів
                using (Transaction transaction = new Transaction(famDoc, "Create Family Parameters"))
                {
                    transaction.Start();
                    
                    try
                    {
                        // Створюємо параметри сімейства
                        CreateFamilyParameters(famDoc, family, options);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        progressReporter?.ReportError($"Failed to create family parameters: {ex.Message}", ex);
                        return (family, ConversionResult.Failed);
                    }
                }

                progressReporter?.ReportStatus("Creating family geometry...", 50);
                
                // Створюємо геометрію сімейства
                ConversionResult geometryResult = await CreateFamilyGeometryAsync(
                    famDoc, family, options, progressReporter, cancellationToken);
                
                if (geometryResult != ConversionResult.Success)
                {
                    return (family, geometryResult);
                }

                progressReporter?.ReportStatus("Creating family types...", 70);
                
                // Транзакція для створення типів сімейства
                using (Transaction transaction = new Transaction(famDoc, "Create Family Types"))
                {
                    transaction.Start();
                    
                    try
                    {
                        // Створюємо типи сімейства
                        CreateFamilyTypes(famDoc, family, options);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        progressReporter?.ReportError($"Failed to create family types: {ex.Message}", ex);
                        return (family, ConversionResult.Failed);
                    }
                }

                progressReporter?.ReportStatus("Saving family document...", 90);
                
                // Визначаємо шлях для збереження
                family.FilePath = DetermineFamilyOutputPath(family.Name, options);
                
                // Зберігаємо документ сімейства
                SaveAsOptions saveOptions = new SaveAsOptions();
                saveOptions.OverwriteExistingFile = true;
                
                try
                {
                    famDoc.SaveAs(family.FilePath, saveOptions);
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportError($"Failed to save family document: {ex.Message}", ex);
                    return (family, ConversionResult.FileWriteError);
                }

                progressReporter?.ReportStatus("Family generation completed successfully.", 100);
                progressReporter?.ReportCompletion($"Family saved to {family.FilePath}", true);

                return (family, ConversionResult.Success);
            }
            catch (OperationCanceledException)
            {
                progressReporter?.ReportStatus("Family generation cancelled.", 0);
                return (null, ConversionResult.Cancelled);
            }
            catch (Exception ex)
            {
                progressReporter?.ReportError($"Error during family generation: {ex.Message}", ex);
                return (null, ConversionResult.Failed);
            }
        }

        private string DetermineFamilyTemplate(string familyCategory, ConversionOptions options)
        {
            // Якщо шаблон вказано в опціях, використовуємо його
            if (!string.IsNullOrEmpty(options.TemplateFilePath) && File.Exists(options.TemplateFilePath))
            {
                return options.TemplateFilePath;
            }

            // Визначаємо шлях до теки шаблонів Revit
            string revitVersion = options.RevitVersion;
            string templateRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Autodesk", $"RVT {revitVersion}", "Family Templates", "English");
            
            // Якщо вказана категорія не існує, використовуємо Generic Model
            string templateFileName;
            switch (familyCategory.ToLower())
            {
                case "walls":
                    templateFileName = "Wall.rft";
                    break;
                case "floors":
                    templateFileName = "Floor.rft";
                    break;
                case "ceilings":
                    templateFileName = "Ceiling.rft";
                    break;
                case "doors":
                    templateFileName = "Door.rft";
                    break;
                case "windows":
                    templateFileName = "Window.rft";
                    break;
                case "furniture":
                    templateFileName = "Furniture.rft";
                    break;
                default:
                    templateFileName = "Generic Model.rft";
                    break;
            }
            
            string templatePath = Path.Combine(templateRoot, templateFileName);
            
            // Перевіряємо, чи існує файл
            if (File.Exists(templatePath))
            {
                return templatePath;
            }
            
            // Якщо файл не знайдено, використовуємо Generic Model
            return Path.Combine(templateRoot, "Generic Model.rft");
        }

        private string DetermineFamilyOutputPath(string familyName, ConversionOptions options)
        {
            if (!string.IsNullOrEmpty(options.OutputFilePath))
            {
                return options.OutputFilePath;
            }

            // Генеруємо шлях за замовчуванням
            return Path.Combine(Path.GetTempPath(), $"{familyName}_{Guid.NewGuid()}.rfa");
        }

        private void CreateFamilyParameters(Document document, RevitFamily family, ConversionOptions options)
        {
            if (document == null || family == null)
                return;
            
            // Отримуємо менеджер параметрів сімейства
            FamilyManager familyManager = document.FamilyManager;
            
            // Створюємо параметри
            foreach (var param in family.Parameters)
            {
                try
                {
                    // Визначаємо тип параметра
                    ParameterType paramType = ParameterType.Text;
                    if (param.Value is double)
                        paramType = ParameterType.Length;
                    else if (param.Value is int)
                        paramType = ParameterType.Integer;
                    else if (param.Value is bool)
                        paramType = ParameterType.YesNo;
                    
                    // Визначаємо групу параметра
                    BuiltInParameterGroup paramGroup = BuiltInParameterGroup.PG_GENERAL;
                    
                    // Перевіряємо, чи існує параметр
                    FamilyParameter existingParam = familyManager.get_Parameter(param.Key);
                    if (existingParam == null)
                    {
                        // Створюємо новий параметр
                        FamilyParameter newParam = familyManager.AddParameter(
                            param.Key, 
                            paramGroup, 
                            paramType, 
                            false // Не екземпляр (тобто параметр типу)
                        );
                        
                        // Встановлюємо формулу або значення
                        if (param.Value is string formula && formula.StartsWith("="))
                        {
                            familyManager.SetFormula(newParam, formula.Substring(1));
                        }
                        else
                        {
                            SetParameterValue(familyManager, newParam, param.Value);
                        }
                    }
                    else
                    {
                        // Встановлюємо значення існуючого параметра
                        SetParameterValue(familyManager, existingParam, param.Value);
                    }
                }
                catch (Exception ex)
                {
                    // Логуємо помилку та продовжуємо
                    _progressReporter?.ReportWarning($"Failed to create parameter {param.Key}: {ex.Message}");
                }
            }
        }

        private void SetParameterValue(FamilyManager familyManager, FamilyParameter parameter, object value)
        {
            if (parameter == null || value == null)
                return;
            
            try
            {
                switch (parameter.Definition.ParameterType)
                {
                    case ParameterType.Length:
                    case ParameterType.Area:
                    case ParameterType.Volume:
                    case ParameterType.Angle:
                        if (double.TryParse(value.ToString(), out double doubleValue))
                        {
                            familyManager.Set(parameter, doubleValue);
                        }
                        break;
                    case ParameterType.Integer:
                        if (int.TryParse(value.ToString(), out int intValue))
                        {
                            familyManager.Set(parameter, intValue);
                        }
                        break;
                    case ParameterType.YesNo:
                        if (bool.TryParse(value.ToString(), out bool boolValue))
                        {
                            familyManager.Set(parameter, boolValue ? 1 : 0);
                        }
                        break;
                    default:
                        familyManager.Set(parameter, value.ToString());
                        break;
                }
            }
            catch (Exception)
            {
                // Ігноруємо помилки при встановленні значення
            }
        }

        private async Task<ConversionResult> CreateFamilyGeometryAsync(
            Document document, 
            RevitFamily family, 
            ConversionOptions options, 
            IProgressReporter progressReporter,
            CancellationToken cancellationToken)
        {
            if (document == null || family == null || family.GeometryModels == null || family.GeometryModels.Count == 0)
            {
                progressReporter?.ReportWarning("No geometry models to create in family.");
                return ConversionResult.PartialSuccess;
            }

            // Обробляємо кожну модель геометрії
            int total = family.GeometryModels.Count;
            int current = 0;
            int successful = 0;

            foreach (var model in family.GeometryModels)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ConversionResult.Cancelled;
                }
                
                current++;
                double progress = 50 + (current / (double)total) * 20;
                progressReporter?.ReportStatus($"Processing geometry model {current} of {total}: {model.Name}", progress);

                try
                {
                    // Створюємо новий перегляд для розміщення геометрії
                    using (Transaction transaction = new Transaction(document, $"Create Geometry: {model.Name}"))
                    {
                        transaction.Start();

                        // Обробляємо геометрію в залежності від типу
                        switch (model.GeometryType)
                        {
                            case GeometryType.Brep:
                                if (CreateBrepGeometry(document, model))
                                {
                                    successful++;
                                }
                                break;
                                
                            case GeometryType.Mesh:
                                if (CreateMeshGeometry(document, model))
                                {
                                    successful++;
                                }
                                break;
                                
                            default:
                                progressReporter?.ReportWarning($"Unsupported geometry type: {model.GeometryType}");
                                break;
                        }

                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportError($"Error creating geometry for {model.Name}: {ex.Message}", ex);
                }
            }

            // Повертаємо результат конвертації
            if (successful == 0)
            {
                return ConversionResult.Failed;
            }
            else if (successful < total)
            {
                return ConversionResult.PartialSuccess;
            }
            else
            {
                return ConversionResult.Success;
            }
        }

        private bool CreateBrepGeometry(Document document, GeometryModel model)
        {
            // Створення BRep геометрії у сімействі
            if (model.GeometryData is Solid solid)
            {
                // Створення геометрії з Solid
                // Отримуємо редактор сімейства
                FamilyItemFactory factory = document.FamilyCreate;
                
                // Створюємо форму через тверде тіло
                GenericForm form = factory.NewGenericForm(true);
                
                // Додаємо тверде тіло до форми
                form.AddSolid(solid);
                
                model.RevitObject = form;
                return true;
            }
            else if (model.GeometryData is List<Curve> curves)
            {
                // Створення геометрії з кривих
                // Отримуємо активний перегляд
                View view = document.ActiveView;
                if (view == null)
                {
                    // Створюємо новий перегляд, якщо активний відсутній
                    ViewFamilyType viewFamilyType = new FilteredElementCollector(document)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
                    
                    view = View3D.CreateIsometric(document, viewFamilyType.Id);
                }
                
                // Створюємо площину ескізу
                Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketchPlane = SketchPlane.Create(document, plane);
                
                // Створюємо криві в моделі
                List<ModelCurve> modelCurves = new List<ModelCurve>();
                foreach (Curve curve in curves)
                {
                    ModelCurve modelCurve = document.FamilyCreate.NewModelCurve(curve, sketchPlane);
                    modelCurves.Add(modelCurve);
                }
                
                model.RevitObject = modelCurves;
                return true;
            }
            
            return false;
        }

        private bool CreateMeshGeometry(Document document, GeometryModel model)
        {
            // Створення Mesh геометрії у сімействі
            if (model.GeometryData is Mesh mesh)
            {
                // Отримуємо редактор сімейства
                FamilyItemFactory factory = document.FamilyCreate;
                
                // Створюємо форму
                GenericForm form = factory.NewGenericForm(true);
                
                // Додаємо меш до форми через DirectShape
                DirectShape ds = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { mesh });
                
                model.RevitObject = ds;
                return true;
            }
            else if (model.GeometryData is TessellatedShapeBuilder builder)
            {
                // Будуємо тесельовану геометрію
                builder.Build();
                TessellatedShapeBuilderResult result = builder.GetBuildResult();
                
                if (result.GetGeometricalObjects().Count > 0)
                {
                    // Створюємо DirectShape з тесельованої геометрії
                    DirectShape ds = DirectShape.CreateElement(document, new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.SetShape(result.GetGeometricalObjects());
                    
                    model.RevitObject = ds;
                    return true;
                }
            }
            
            return false;
        }

        private void CreateFamilyTypes(Document document, RevitFamily family, ConversionOptions options)
        {
            if (document == null || family == null || family.TypeParameters == null || family.TypeParameters.Count == 0)
            {
                // Якщо немає типів, створюємо один тип за замовчуванням
                FamilyManager familyManager = document.FamilyManager;
                
                if (familyManager.Types.Size == 0)
                {
                    familyManager.NewType("Default");
                }
                
                return;
            }

            // Отримуємо менеджер сімейства
            FamilyManager familyManager = document.FamilyManager;
            
            // Створюємо кожен тип
            foreach (var typeParams in family.TypeParameters)
            {
                try
                {
                    // Отримуємо ім'я типу
                    string typeName = "Default";
                    if (typeParams.TryGetValue("Type Name", out object nameObj) && nameObj != null)
                    {
                        typeName = nameObj.ToString();
                    }
                    
                    // Перевіряємо, чи існує тип
                    FamilyType existingType = null;
                    foreach (FamilyType type in familyManager.Types)
                    {
                        if (type.Name == typeName)
                        {
                            existingType = type;
                            break;
                        }
                    }
                    
                    // Створюємо новий тип, якщо не існує
                    FamilyType currentType;
                    if (existingType == null)
                    {
                        currentType = familyManager.NewType(typeName);
                    }
                    else
                    {
                        currentType = existingType;
                        familyManager.CurrentType = currentType;
                    }
                    
                    // Встановлюємо параметри для типу
                    foreach (var param in typeParams)
                    {
                        if (param.Key != "Type Name" && param.Value != null)
                        {
                            // Отримуємо параметр
                            FamilyParameter familyParam = familyManager.get_Parameter(param.Key);
                            if (familyParam != null)
                            {
                                // Встановлюємо значення
                                SetParameterValue(familyManager, familyParam, param.Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логуємо помилку та продовжуємо
                    _progressReporter?.ReportWarning($"Failed to create type: {ex.Message}");
                }
            }
        }
    }
}