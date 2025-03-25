using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RevitConverter.Core.Interfaces
{
    /// <summary>
    /// Інтерфейс для генерації файлів Revit з конвертованої геометрії.
    /// </summary>
    public interface IFileGenerator
    {
        /// <summary>
        /// Генерує файл Revit з наданих моделей геометрії.
        /// </summary>
        /// <param name="models">Колекція моделей геометрії для включення у файл.</param>
        /// <param name="options">Параметри конвертації.</param>
        /// <param name="progressReporter">Інтерфейс для звітування про прогрес.</param>
        /// <param name="cancellationToken">Токен для скасування операції.</param>
        /// <returns>Шлях до створеного файлу та результат операції.</returns>
        Task<(string FilePath, ConversionResult Result)> GenerateFileAsync(
            IEnumerable<GeometryModel> models, 
            ConversionOptions options, 
            IProgressReporter progressReporter, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Генерує родину Revit з наданого визначення родини.
        /// </summary>
        /// <param name="family">Визначення родини для генерації.</param>
        /// <param name="options">Параметри конвертації.</param>
        /// <param name="progressReporter">Інтерфейс для звітування про прогрес.</param>
        /// <param name="cancellationToken">Токен для скасування операції.</param>
        /// <returns>Шлях до створеного файлу родини та результат операції.</returns>
        Task<(string FilePath, ConversionResult Result)> GenerateFamilyAsync(
            RevitFamily family, 
            ConversionOptions options, 
                IProgressReporter progressReporter, 
            CancellationToken cancellationToken = default);
    }
}