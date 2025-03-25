using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace RevitConverter.Core.Interfaces
{
    /// <summary>
    /// Інтерфейс для конвертації геометричних даних у формати, сумісні з Revit.
    /// </summary>
    public interface IGeometryConverter
    {
        /// <summary>
        /// Конвертує надану модель геометрії у формат, сумісний з Revit.
        /// </summary>
        /// <param name="model">Модель геометрії для конвертації.</param>
        /// <param name="options">Параметри конвертації.</param>
        /// <param name="progressReporter">Інтерфейс для звітування про прогрес.</param>
        /// <param name="cancellationToken">Токен для скасування операції.</param>
        /// <returns>Результат операції конвертації.</returns>
        Task<ConversionResult> ConvertAsync(
            GeometryModel model, 
            ConversionOptions options, 
            IProgressReporter progressReporter, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Перевіряє, чи може цей конвертер обробляти наданий тип геометрії.
        /// </summary>
        /// <param name="geometryType">Тип геометрії для перевірки.</param>
        /// <returns>True, якщо цей конвертер може обробляти тип геометрії.</returns>
        bool CanConvert(GeometryType geometryType);
    }
}