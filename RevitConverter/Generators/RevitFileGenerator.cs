using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using RevitConverter.Core.Enums;
using RevitConverter.Core.Interfaces;
using RevitConverter.Core.Models;
using RevitConverter.Extensions;

namespace RevitConverter.Generators
{
    public class RevitFileGenerator : IFileGenerator
    {
        private readonly IProgressReporter _progressReporter;

        public RevitFileGenerator(IProgressReporter progressReporter = null)
        {
            _progressReporter = progressReporter;
        }

        public async Task<(string FilePath, ConversionResult Result)> GenerateFileAsync(
            IEnumerable<GeometryModel> models, 
            ConversionOptions options, 
            IProgressReporter progressReporter, 
            CancellationToken cancellationToken = default)
        {
            progressReporter?.ReportStatus("Starting Revit file generation...", 0);

            try
            {
                if (models == null || !models.Any())
                {
                    progressReporter?.ReportError("No geometry models provided.");
                    return (null, ConversionResult.InvalidInput);
                }

                string outputPath = options.OutputFilePath;
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Path.GetTempPath(), $"RevitConverter_{Guid.NewGuid()}.rvt");
                    progressReporter?.ReportStatus($"No output path provided, using temporary path: {outputPath}", 5);
                }

                progressReporter?.ReportStatus("Creating Revit document...", 10);
                
                // Отримуємо додаток Revit
                var app = options.RevitApplication as Application;
                if (app == null)
                {
                    progressReporter?.ReportError("Invalid Revit application object.");
                    return (null, ConversionResult.RevitError);
                }
                
                // Створюємо новий документ Revit
                Document doc = null;
                
                // Визначаємо шлях до шаблону
                string templatePath = options.TemplateFilePath;
                if (string.IsNullOrEmpty(templatePath))
                {
                    // За замовчуванням використовуємо стандартний шаблон
                    templatePath = app.DefaultProjectTemplate;
                    
                    if (string.IsNullOrEmpty(templatePath))
                    {
                        progressReporter?.ReportError("Default project template not found.");
                        return (null, ConversionResult.InvalidInput);
                    }
                }
                
                // Створюємо документ
                doc = app.NewProjectDocument(templatePath);
                
                // Зберігаємо документ у опціях для подальшого використання
                options.RevitDocument = doc;

                progressReporter?.ReportStatus("Placing elements in document...", 30);
                
                // Створюємо транзакцію для розміщення елементів
                using (Transaction transaction = new Transaction(doc, "Place Elements"))
                {
                    transaction.Start();
                    
                    try
                    {
                        // Розміщуємо моделі геометрії у документі
                        if (!PlaceElements(doc, models, options, progressReporter))
                        {
                            transaction.RollBack();
                            progressReporter?.ReportError("Failed to place elements in document.");
                            return (null, ConversionResult.Failed);
                        }
                        
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        progressReporter?.ReportError($"Error placing elements: {ex.Message}", ex);
                        return (null, ConversionResult.Failed);
                    }
                }

                progressReporter?.ReportStatus("Saving Revit document...", 80);
                
                // Зберігаємо документ
                SaveAsOptions saveOptions = new SaveAsOptions();
                saveOptions.OverwriteExistingFile = true;
                
                try
                {
                    doc.SaveAs(outputPath, saveOptions);
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportError($"Failed to save document: {ex.Message}", ex);
                    return (null, ConversionResult.FileWriteError);
                }

                progressReporter?.ReportStatus("Revit file generation completed successfully.", 100);
                progressReporter?.ReportCompletion($"Revit file saved to {outputPath}", true);

                return (outputPath, ConversionResult.Success);
            }
            catch (OperationCanceledException)
            {
                progressReporter?.ReportStatus("Revit file generation cancelled.", 0);
                return (null, ConversionResult.Cancelled);
            }
            catch (Exception ex)
            {
                progressReporter?.ReportError($"Error during Revit file generation: {ex.Message}", ex);
                return (null, ConversionResult.Failed);
            }
        }

        public async Task<(string FilePath, ConversionResult Result)> GenerateFamilyAsync(
            RevitFamily family, 
            ConversionOptions options, 
            IProgressReporter progressReporter, 
            CancellationToken cancellationToken = default)
        {
            progressReporter?.ReportStatus("Starting Revit family generation...", 0);

            try
            {
                if (family == null || !family.IsValid)
                {
                    progressReporter?.ReportError("Invalid family definition.");
                    return (null, ConversionResult.InvalidInput);
                }

                string outputPath = options.OutputFilePath;
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Path.GetTempPath(), $"{family.Name}_{Guid.NewGuid()}.rfa");
                    progressReporter?.ReportStatus($"No output path provided, using temporary path: {outputPath}", 5);
                }

                progressReporter?.ReportStatus("Creating family document...", 10);
                
                // Отримуємо додаток Revit
                var app = options.RevitApplication as Application;
                if (app == null)
                {
                    progressReporter?.ReportError("Invalid Revit application object.");
                    return (null, ConversionResult.RevitError);
                }
                
                // Перевіряємо наявність шаблону сімейства
                string templatePath = family.TemplateFile;
                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    progressReporter?.ReportError($"Family template not found: {templatePath}");
                    return (null, ConversionResult.InvalidInput);
                }
                
                // Створюємо документ сімейства
                Document famDoc = null;
                try
                {
                    // У різних версіях Revit метод може відрізнятися
                    try
                    {
                        famDoc = app.NewFamilyDocument(templatePath);
                    }
                    catch
                    {
                        // Альтернативний варіант для інших версій Revit
                        
                    }
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportError($"Failed to create family document: {ex.Message}", ex);
                    return (null, ConversionResult.RevitError);
                }
                
                if (famDoc == null)
                {
                    progressReporter?.ReportError("Failed to create family document.");
                    return (null, ConversionResult.RevitError);
                }
                
                // Зберігаємо документ у опціях та у сімействі
                options.RevitDocument = famDoc;
                family.RevitDocument = famDoc;

                progressReporter?.ReportStatus("Creating family parameters...", 20);
                
                // Створюємо параметри сімейства
                using (Transaction transaction = new Transaction(famDoc, "Create Family Parameters"))
                {
                    transaction.Start();
                    
                    try
                    {
                        CreateFamilyParameters(famDoc, family, options);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        progressReporter?.ReportError($"Failed to create family parameters: {ex.Message}", ex);
                    }
                }

                progressReporter?.ReportStatus("Creating family geometry...", 40);
                
                // Створюємо геометрію сімейства
                using (Transaction transaction = new Transaction(famDoc, "Create Family Geometry"))
                {
                    transaction.Start();
                    
                    try
                    {
                        PlaceElements(famDoc, family.GeometryModels, options, progressReporter);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        progressReporter?.ReportError($"Failed to create family geometry: {ex.Message}", ex);
                    }
                }

                progressReporter?.ReportStatus("Creating family types...", 70);
                
                // Створюємо типи сімейства
                using (Transaction transaction = new Transaction(famDoc, "Create Family Types"))
                {
                    transaction.Start();
                    
                    try
                    {
                        CreateFamilyTypes(famDoc, family, options);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.RollBack();
                        progressReporter?.ReportError($"Failed to create family types: {ex.Message}", ex);
                    }
                }

                progressReporter?.ReportStatus("Saving family document...", 90);
                
                // Зберігаємо документ
                SaveAsOptions saveOptions = new SaveAsOptions();
                saveOptions.OverwriteExistingFile = true;
                
                try
                {
                    famDoc.SaveAs(outputPath, saveOptions);
                    family.FilePath = outputPath;
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportError($"Failed to save family document: {ex.Message}", ex);
                    return (null, ConversionResult.FileWriteError);
                }

                progressReporter?.ReportStatus("Revit family generation completed successfully.", 100);
                progressReporter?.ReportCompletion($"Revit family saved to {outputPath}", true);

                return (outputPath, ConversionResult.Success);
            }
            catch (OperationCanceledException)
            {
                progressReporter?.ReportStatus("Revit family generation cancelled.", 0);
                return (null, ConversionResult.Cancelled);
            }
            catch (Exception ex)
            {
                progressReporter?.ReportError($"Error during Revit family generation: {ex.Message}", ex);
                return (null, ConversionResult.Failed);
            }
        }

        private bool PlaceElements(Document document, IEnumerable<GeometryModel> models, ConversionOptions options, IProgressReporter progressReporter)
        {
            if (document == null || models == null || !models.Any())
            {
                return false;
            }

            int count = 0;
            int total = models.Count();
            
            foreach (var model in models)
            {
                count++;
                double progress = 30 + (count / (double)total) * 50;
                progressReporter?.ReportStatus($"Placing element {count} of {total}: {model.Name}", progress);
                
                try
                {
                    // Конвертуємо модель у елемент Revit
                    Element element = null;
                    
                    // Визначаємо підходящий тип елемента
                    switch (model.ElementType)
                    {
                        case RevitElementType.Wall:
                            element = CreateWall(document, model);
                            break;
                        case RevitElementType.Floor:
                            element = CreateFloor(document, model);
                            break;
                        case RevitElementType.Ceiling:
                            element = CreateCeiling(document, model);
                            break;
                        case RevitElementType.Roof:
                            element = CreateRoof(document, model);
                            break;
                        default:
                            // За замовчуванням створюємо DirectShape
                            element = model.ToDirectShape(document);
                            break;
                    }
                    
                    // Зберігаємо посилання на створений елемент
                    if (element != null)
                    {
                        model.RevitObject = element;
                        
                        // Встановлюємо параметри елемента
                        foreach (var param in model.Parameters)
                        {
                            try
                            {
                                Parameter revitParam = element.LookupParameter(param.Key);
                                if (revitParam != null && !revitParam.IsReadOnly)
                                {
                                    // Встановлюємо значення параметра
                                    switch (revitParam.StorageType)
                                    {
                                        case StorageType.Double:
                                            if (double.TryParse(param.Value.ToString(), out double doubleValue))
                                            {
                                                revitParam.Set(doubleValue);
                                            }
                                            break;
                                        case StorageType.Integer:
                                            if (int.TryParse(param.Value.ToString(), out int intValue))
                                            {
                                                revitParam.Set(intValue);
                                            }
                                            break;
                                        case StorageType.String:
                                            revitParam.Set(param.Value.ToString());
                                            break;
                                        case StorageType.ElementId:
                                            // ElementId параметри потребують спеціальної обробки
                                            break;
                                    }
                                }
                            }
                            catch
                            {
                                // Ігноруємо помилки при встановленні параметрів
                            }
                        }
                    }
                    else
                    {
                        progressReporter?.ReportWarning($"Failed to create element for {model.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    progressReporter?.ReportWarning($"Error placing element {model.Name}: {ex.Message}");
                }
            }
            
            return true;
        }

        private Element CreateWall(Document document, GeometryModel model)
        {
            try
            {
                // Отримуємо геометрію для створення стіни
                if (model.GeometryData is Curve curve)
                {
                    // Отримуємо рівень
                    Level level = new FilteredElementCollector(document)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault();
                    
                    if (level == null)
                    {
                        // Якщо рівень не знайдено, створюємо новий
                        using (Transaction t = new Transaction(document, "Create Level"))
                        {
                            t.Start();
                            level = Level.Create(document, 0);
                            t.Commit();
                        }
                    }
                    
                    // Отримуємо тип стіни за замовчуванням
                    WallType wallType = new FilteredElementCollector(document)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault();
                    
                    if (wallType == null)
                    {
                        // Якщо не знайдено жодного типу стіни, повертаємо null
                        return null;
                    }
                    
                    // Визначаємо висоту стіни
                    double height = 3.0; // За замовчуванням 3 метри
                    
                    // Перевіряємо, чи є параметр висоти
                    if (model.Parameters.TryGetValue("Height", out object heightObj) && 
                        double.TryParse(heightObj.ToString(), out double parsedHeight))
                    {
                        height = parsedHeight;
                    }
                    
                    // Створюємо стіну
                    Wall wall = Wall.Create(
                        document, 
                        curve, 
                        wallType.Id, 
                        level.Id, 
                        height, 
                        0, // Офсет
                        false, // Фліп
                        false // Структурна
                    );
                    
                    return wall;
                }
                else
                {
                    // Якщо геометрія не є кривою, створюємо DirectShape
                    return model.ToDirectShape(document);
                }
            }
            catch
            {
                return null;
            }
        }

        private Element CreateFloor(Document document, GeometryModel model)
        {
            try
            {
                // Отримуємо геометрію для створення підлоги
                if (model.GeometryData is CurveLoop curveLoop)
                {
                    // Отримуємо рівень
                    Level level = new FilteredElementCollector(document)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault();
                    
                    if (level == null)
                    {
                        // Якщо рівень не знайдено, створюємо новий
                        using (Transaction t = new Transaction(document, "Create Level"))
                        {
                            t.Start();
                            level = Level.Create(document, 0);
                            t.Commit();
                        }
                    }
                    
                    // Отримуємо тип підлоги за замовчуванням
                    FloorType floorType = new FilteredElementCollector(document)
                        .OfClass(typeof(FloorType))
                        .Cast<FloorType>()
                        .FirstOrDefault();
                    
                    if (floorType == null)
                    {
                        // Якщо не знайдено жодного типу підлоги, повертаємо null
                        return null;
                    }
                    
                    // Створюємо підлогу
                    Floor floor = Floor.Create(
                        document, 
                        new List<CurveLoop> { curveLoop }, 
                        floorType.Id, 
                        level.Id
                    );
                    
                    return floor;
                }
                else
                {
                    // Якщо геометрія не є CurveLoop, створюємо DirectShape
                    return model.ToDirectShape(document);
                }
            }
            catch
            {
                return null;
            }
        }

        private Element CreateCeiling(Document document, GeometryModel model)
        {
            try
            {
                // Отримуємо геометрію для створення стелі
                if (model.GeometryData is CurveLoop curveLoop)
                {
                    // Отримуємо рівень
                    Level level = new FilteredElementCollector(document)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault();
                    
                    if (level == null)
                    {
                        // Якщо рівень не знайдено, створюємо новий
                        using (Transaction t = new Transaction(document, "Create Level"))
                        {
                            t.Start();
                            level = Level.Create(document, 0);
                            t.Commit();
                        }
                    }
                    
                    // Отримуємо тип стелі за замовчуванням
                    CeilingType ceilingType = new FilteredElementCollector(document)
                        .OfClass(typeof(CeilingType))
                        .Cast<CeilingType>()
                        .FirstOrDefault();
                    
                    if (ceilingType == null)
                    {
                        // Якщо не знайдено жодного типу стелі, повертаємо null
                        return null;
                    }
                    
                    // Створюємо стелю
                    Ceiling ceiling = Ceiling.Create(
                        document, 
                        new List<CurveLoop> { curveLoop }, 
                        ceilingType.Id, 
                        level.Id
                    );
                    
                    return ceiling;
                }
                else
                {
                    // Якщо геометрія не є CurveLoop, створюємо DirectShape
                    return model.ToDirectShape(document);
                }
            }
            catch
            {
                return null;
            }
        }

        private Element CreateRoof(Document document, GeometryModel model)
        {
            try
            {
                // Для даху часто потрібна складна геометрія
                // Для спрощення, використовуємо DirectShape
                return model.ToDirectShape(document);
            }
            catch
            {
                return null;
            }
        }

        private void CreateFamilyParameters(Document document, RevitFamily family, ConversionOptions options)
        {
            if (document == null || family == null || family.Parameters == null || family.Parameters.Count == 0)
            {
                return;
            }

            try
            {
                // Отримуємо менеджер сімейства
                FamilyManager familyManager = document.FamilyManager;
                
                // Додаємо параметри
                foreach (var param in family.Parameters)
                {
                    try
                    {
                        // Перевіряємо, чи параметр вже існує
                        FamilyParameter existingParam = familyManager.get_Parameter(param.Key);
                        if (existingParam == null)
                        {
                            // Визначаємо групу параметра
                            BuiltInParameterGroup paramGroup = BuiltInParameterGroup.PG_GENERAL;
                            
                            // Визначаємо тип параметра на основі значення
                            // Тут використовуємо ForgeTypeId для сумісності з новими версіями Revit
                            Category category = null;
                            
                            // Створюємо новий параметр
                            FamilyParameter newParam;
                            
                            // Спроба створити параметр з урахуванням різних версій Revit API
                            try
                            {
                                // Для Revit 2022+
                                ForgeTypeId paramType;
                                
                                if (param.Value is double)
                                    paramType = SpecTypeId.Length;
                                else if (param.Value is int)
                                    paramType = SpecTypeId.Integer;
                                else if (param.Value is bool)
                                    paramType = SpecTypeId.Boolean;
                                else
                                    paramType = SpecTypeId.String.Text;
                                
                                newParam = familyManager.AddParameter(
                                    param.Key,
                                    paramGroup,
                                    paramType,
                                    false // Не екземпляр (тобто параметр типу)
                                );
                            }
                            catch
                            {
                                // Для Revit 2021 і раніше
                                // (потребує компіляції з відповідними DLL)
                                // Використовуємо альтернативний підхід
                                newParam = familyManager.AddParameter(
                                    param.Key,
                                    paramGroup,
                                    category,
                                    false // Не екземпляр (тобто параметр типу)
                                );
                            }
                            
                            // Встановлюємо значення параметра, якщо він був створений
                            if (newParam != null && param.Value != null)
                            {
                                SetFamilyParameterValue(familyManager, newParam, param.Value);
                            }
                        }
                        else
                        {
                            // Якщо параметр вже існує, встановлюємо його значення
                            SetFamilyParameterValue(familyManager, existingParam, param.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _progressReporter?.ReportWarning($"Failed to create parameter {param.Key}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _progressReporter?.ReportError($"Error creating family parameters: {ex.Message}", ex);
            }
        }

        private void SetFamilyParameterValue(FamilyManager familyManager, FamilyParameter parameter, object value)
        {
            if (parameter == null || value == null)
                return;
            
            try
            {
                StorageType storageType = parameter.StorageType;
                
                switch (storageType)
                {
                    case StorageType.Double:
                        if (double.TryParse(value.ToString(), out double doubleValue))
                        {
                            familyManager.Set(parameter, doubleValue);
                        }
                        break;
                    case StorageType.Integer:
                        if (int.TryParse(value.ToString(), out int intValue))
                        {
                            familyManager.Set(parameter, intValue);
                        }
                        break;
                    case StorageType.String:
                        familyManager.Set(parameter, value.ToString());
                        break;
                    case StorageType.ElementId:
                        // ElementId параметри потребують спеціальної обробки
                        break;
                }
            }
            catch
            {
                // Ігноруємо помилки при встановленні значення
            }
        }

        private void CreateFamilyTypes(Document document, RevitFamily family, ConversionOptions options)
        {
            if (document == null || family == null || family.TypeParameters == null || family.TypeParameters.Count == 0)
            {
                return;
            }

            try
            {
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
                        
                        // Перевіряємо, чи тип вже існує
                        FamilyType existingType = null;
                        foreach (FamilyType type in familyManager.Types)
                        {
                            if (type.Name == typeName)
                            {
                                existingType = type;
                                break;
                            }
                        }
                        
                        // Створюємо новий тип або використовуємо існуючий
                        FamilyType currentType;
                        if (existingType == null)
                        {
                            currentType = familyManager.NewType(typeName);
                        }
                        else
                        {
                            currentType = existingType;
                        }
                        
                        // Встановлюємо поточний тип
                        familyManager.CurrentType = currentType;
                        
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
                                    SetFamilyParameterValue(familyManager, familyParam, param.Value);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _progressReporter?.ReportWarning($"Failed to create family type: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _progressReporter?.ReportError($"Error creating family types: {ex.Message}", ex);
            }
        }
    }
}