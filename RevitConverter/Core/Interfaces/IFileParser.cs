using RevitConverter.Core.Enums;
using RevitConverter.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RevitConverter.Core.Interfaces
{
    /// <summary>
    /// Interface for parsing Revit files (.rvt and .rfa) into geometry models.
    /// Contains:
    /// - ParseFileAsync: Parses a Revit file into a collection of geometry models.
    /// - ParseFamilyAsync: Parses a Revit family file into a RevitFamily object.
    /// - CanParseFile: Checks if this parser can handle the given file.
    /// </summary>
    public interface IFileParser
    {
        /// <summary>
        /// Parses a Revit file into a collection of geometry models.
        /// </summary>
        /// <param name="filePath">Path to the Revit file.</param>
        /// <param name="options">Conversion options.</param>
        /// <param name="progressReporter">Interface for reporting progress.</param>
        /// <param name="cancellationToken">Token for cancelling the operation.</param>
        /// <returns>Collection of geometry models and result of the parsing operation.</returns>
        Task<(IEnumerable<GeometryModel> Models, ConversionResult Result)> ParseFileAsync(
            string filePath,
            ConversionOptions options,
            IProgressReporter progressReporter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses a Revit family file into a RevitFamily object.
        /// </summary>
        /// <param name="filePath">Path to the Revit family file.</param>
        /// <param name="options">Conversion options.</param>
        /// <param name="progressReporter">Interface for reporting progress.</param>
        /// <param name="cancellationToken">Token for cancelling the operation.</param>
        /// <returns>RevitFamily object and result of the parsing operation.</returns>
        Task<(RevitFamily Family, ConversionResult Result)> ParseFamilyAsync(
            string filePath,
            ConversionOptions options,
            IProgressReporter progressReporter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if this parser can handle the given file.
        /// </summary>
        /// <param name="filePath">Path to the file to check.</param>
        /// <returns>True if this parser can handle the file.</returns>
        bool CanParseFile(string filePath);
    }
}