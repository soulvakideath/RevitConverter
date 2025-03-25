using Autodesk.Revit.DB;
using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;

namespace RevitConverter.Extensions
{
    public static class RevitExtensions
    {
        public static DirectShape ToDirectShape(this GeometryModel model, Document document)
        {
            if (model == null || model.GeometryData == null || document == null)
            {
                return null;
            }

            try
            {
                // Створюємо транзакцію
                using (var transaction = new Transaction(document, "Create DirectShape"))
                {
                    transaction.Start();
                    
                    // Визначаємо категорію
                    ElementId categoryId;
                    switch (model.ElementType)
                    {
                        case RevitElementType.Wall:
                            categoryId = new ElementId(BuiltInCategory.OST_Walls);
                            break;
                        case RevitElementType.Floor:
                            categoryId = new ElementId(BuiltInCategory.OST_Floors);
                            break;
                        case RevitElementType.Ceiling:
                            categoryId = new ElementId(BuiltInCategory.OST_Ceilings);
                            break;
                        case RevitElementType.Roof:
                            categoryId = new ElementId(BuiltInCategory.OST_Roofs);
                            break;
                        default:
                            categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                            break;
                    }
                    
                    // Створюємо DirectShape
                    var directShape = DirectShape.CreateElement(document, categoryId);
                    
                    // Встановлюємо геометрію
                    if (model.GeometryData is GeometryObject geomObj)
                    {
                        directShape.SetShape(new List<GeometryObject> { geomObj });
                    }
                    else if (model.GeometryData is IEnumerable<GeometryObject> geomObjs)
                    {
                        directShape.SetShape(geomObjs.ToList());
                    }
                    else if (model.GeometryData is Solid solid)
                    {
                        directShape.SetShape(new List<GeometryObject> { solid });
                    }
                    
                    // Встановлюємо ім'я
                    if (!string.IsNullOrEmpty(model.Name))
                    {
                        Parameter param = directShape.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                        if (param != null && !param.IsReadOnly)
                        {
                            param.Set(model.Name);
                        }
                    }
                    
                    transaction.Commit();
                    return directShape;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static FamilyInstance ToFamilyInstance(this GeometryModel model, Document document, FamilySymbol familySymbol)
        {
            if (model == null || document == null || familySymbol == null)
            {
                return null;
            }

            try
            {
                // Створюємо транзакцію
                using (var transaction = new Transaction(document, "Create Family Instance"))
                {
                    transaction.Start();
                    
                    // Активуємо символ сімейства, якщо він не активний
                    if (!familySymbol.IsActive)
                    {
                        familySymbol.Activate();
                    }
                    
                    // Визначаємо точку вставки
                    XYZ location = XYZ.Zero;
                    if (model.GeometryData is Solid solid)
                    {
                        // Використовуємо центр ваги тіла як точку вставки
                        location = solid.ComputeCentroid();
                    }
                    
                    // Створюємо екземпляр сімейства
                    FamilyInstance instance = document.Create.NewFamilyInstance(
                        location, 
                        familySymbol, 
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    
                    // Встановлюємо параметри з моделі
                    foreach (var param in model.Parameters)
                    {
                        Parameter revitParam = instance.LookupParameter(param.Key);
                        if (revitParam != null && !revitParam.IsReadOnly)
                        {
                            // Конвертуємо значення параметра в потрібний тип
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
                                    // Обробка ElementId буде залежати від конкретного параметра
                                    break;
                            }
                        }
                    }
                    
                    transaction.Commit();
                    return instance;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static IEnumerable<Element> ToRevitElements(this IEnumerable<GeometryModel> models, Document document)
        {
            if (models == null || document == null)
            {
                yield break;
            }

            foreach (var model in models)
            {
                // Визначаємо тип елемента та створюємо відповідний Revit-елемент
                Element element = null;
                
                try
                {
                    using (var transaction = new Transaction(document, $"Create {model.Name}"))
                    {
                        transaction.Start();
                        
                        switch (model.ElementType)
                        {
                            case RevitElementType.Wall:
                                element = CreateWall(model, document);
                                break;
                            case RevitElementType.Floor:
                                element = CreateFloor(model, document);
                                break;
                            case RevitElementType.Ceiling:
                                element = CreateCeiling(model, document);
                                break;
                            case RevitElementType.Roof:
                                element = CreateRoof(model, document);
                                break;
                            default:
                                // За замовчуванням створюємо DirectShape
                                element = model.ToDirectShape(document);
                                break;
                        }
                        
                        transaction.Commit();
                    }
                }
                catch (Exception)
                {
                    // Логуємо помилку та продовжуємо з наступною моделлю
                    element = null;
                }
                
                if (element != null)
                {
                    yield return element;
                }
            }
        }

        private static Wall CreateWall(GeometryModel model, Document document)
        {
            // Створення стіни вимагає кривої для основи стіни
            if (model.GeometryData is Curve baseCurve)
            {
                // Отримуємо тип стіни за замовчуванням
                FilteredElementCollector collector = new FilteredElementCollector(document);
                collector.OfClass(typeof(WallType));
                WallType wallType = collector.FirstElement() as WallType;
                
                // Створюємо стіну по кривій
                double height = 3.0; // За замовчуванням 3 метри
                
                // Перевіряємо, чи є параметр висоти в моделі
                if (model.Parameters.TryGetValue("Height", out object heightObj))
                {
                    if (double.TryParse(heightObj.ToString(), out double parsedHeight))
                    {
                        height = parsedHeight;
                    }
                }
                
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
                
                Wall wall = Wall.Create(
                    document, 
                    baseCurve, 
                    wallType.Id, 
                    level.Id, 
                    height, 
                    0.0, // Офсет
                    false, // Структурна стіна
                    false // Не перевертати
                );
                
                return wall;
            }
            
            return null;
        }

        private static Floor CreateFloor(GeometryModel model, Document document)
        {
            // Створення підлоги вимагає контуру та рівня
            if (model.GeometryData is CurveLoop curveLoop)
            {
                // Отримуємо тип підлоги за замовчуванням
                FilteredElementCollector collector = new FilteredElementCollector(document);
                collector.OfClass(typeof(FloorType));
                FloorType floorType = collector.FirstElement() as FloorType;
                
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
                
                // Створюємо підлогу
                List<CurveLoop> loopList = new List<CurveLoop> { curveLoop };
                Floor floor = Floor.Create(document, loopList, floorType.Id, level.Id);
                
                return floor;
            }
            
            return null;
        }

        private static Ceiling CreateCeiling(GeometryModel model, Document document)
        {
            // Створення стелі вимагає контуру та рівня
            if (model.GeometryData is CurveLoop curveLoop)
            {
                // Отримуємо тип стелі за замовчуванням
                FilteredElementCollector collector = new FilteredElementCollector(document);
                collector.OfClass(typeof(CeilingType));
                CeilingType ceilingType = collector.FirstElement() as CeilingType;
                
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
                
                // Створюємо стелю
                Ceiling ceiling = Ceiling.Create(document, new List<CurveLoop> { curveLoop }, ceilingType.Id, level.Id);
                
                return ceiling;
            }
            
            return null;
        }

        private static RoofBase CreateRoof(GeometryModel model, Document document)
        {
            // Створення даху вимагає контуру та рівня
            // За замовчуванням створюємо DirectShape
            var directShape = model.ToDirectShape(document);
            return directShape as RoofBase;
        }

        public static RevitElementType GetRevitElementType(this GeometryModel model)
        {
            if (model == null)
            {
                return RevitElementType.GenericModel;
            }

            // Якщо тип вже вказано, використовуємо його
            if (model.ElementType != RevitElementType.GenericModel)
            {
                return model.ElementType;
            }

            // Аналізуємо ім'я для визначення типу елемента
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
            
            // Аналізуємо геометрію для визначення типу
            if (model.GeometryData is Solid solid)
            {
                // Аналіз пропорцій та форми для визначення типу
                BoundingBoxXYZ bbox = solid.GetBoundingBox();
                double width = bbox.Max.X - bbox.Min.X;
                double length = bbox.Max.Y - bbox.Min.Y;
                double height = bbox.Max.Z - bbox.Min.Z;
                
                // Якщо висота значно більша за ширину та довжину, це може бути стіна
                if (height > 2 * width && height > 2 * length)
                {
                    return RevitElementType.Wall;
                }
                // Якщо ширина та довжина значно більші за висоту, це може бути підлога
                else if (width > 5 * height && length > 5 * height)
                {
                    return RevitElementType.Floor;
                }
            }
            
            return RevitElementType.GenericModel;
        }

        public static bool SaveAsRfa(this RevitFamily family, string filePath, bool overwrite = false)
        {
            if (family == null || string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            // Перевіряємо, чи файл існує і чи його можна перезаписати
            if (File.Exists(filePath) && !overwrite)
            {
                return false;
            }

            try
            {
                // Отримуємо документ Revit сімейства
                Document doc = family.RevitDocument as Document;
                if (doc == null)
                    return false;
                
                // Зберігаємо документ
                SaveAsOptions options = new SaveAsOptions();
                options.OverwriteExistingFile = overwrite;
                
                doc.SaveAs(filePath, options);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SaveAsRvt(this Document document, string filePath, bool overwrite = false)
        {
            if (document == null || string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            // Перевіряємо, чи файл існує і чи його можна перезаписати
            if (File.Exists(filePath) && !overwrite)
            {
                return false;
            }

            try
            {
                // Зберігаємо документ
                SaveAsOptions options = new SaveAsOptions();
                options.OverwriteExistingFile = overwrite;
                
                document.SaveAs(filePath, options);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}