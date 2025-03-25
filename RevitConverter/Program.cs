using RevitConverter.Core.Interfaces;
using RevitConverter.Core.Models;
using RevitConverter.Conventers;
using RevitConverter.Generators;
using RevitConverter.Parses;
using RevitConverter.Utilities;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace RevitConverter
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("RevitConverter - Convert geometry models to Revit format");
            
            // Create command line interface
            var rootCommand = new RootCommand("RevitConverter tool for converting geometry to Revit format");
            
            // Add input file option
            var inputFileOption = new Option<string>(
                "--input",
                "Path to the input file to convert"
            );
            inputFileOption.IsRequired = true;
            rootCommand.AddOption(inputFileOption);
            
            // Add output file option
            var outputFileOption = new Option<string>(
                "--output",
                "Path for the output file"
            );
            rootCommand.AddOption(outputFileOption);
            
            // Add Revit version option
            var revitVersionOption = new Option<string>(
                "--revit-version",
                () => "2024",
                "Target Revit version"
            );
            rootCommand.AddOption(revitVersionOption);
            
            // Define the command handler
            rootCommand.SetHandler(async (context) =>
            {
                // Get the option values
                string inputFile = context.ParseResult.GetValueForOption(inputFileOption);
                string outputFile = context.ParseResult.GetValueForOption(outputFileOption);
                string revitVersion = context.ParseResult.GetValueForOption(revitVersionOption);
                
                // Create progress reporter
                var progressReporter = new ProgressReporter();
                progressReporter.StatusChanged += (sender, message) => Console.WriteLine(message);
                progressReporter.ProgressChanged += (sender, percentage) => 
                    Console.WriteLine($"Progress: {percentage:F1}%");
                progressReporter.ErrorOccurred += (sender, error) => 
                    Console.WriteLine($"ERROR: {error.Message}");
                progressReporter.WarningOccurred += (sender, warning) => 
                    Console.WriteLine($"WARNING: {warning}");
                
                // Set up conversion options
                var options = new ConversionOptions
                {
                    RevitVersion = revitVersion,
                    OutputFilePath = outputFile,
                    DetailedProgress = true
                };
                
                try
                {
                    // Create file parser
                    var fileParser = new RevitFileParser(progressReporter);
                    
                    // Parse the input file
                    if (fileParser.CanParseFile(inputFile))
                    {
                        Console.WriteLine("Parsing Revit file...");
                        var result = await fileParser.ParseFileAsync(
                            inputFile, options, progressReporter);
                        
                        var models = result.Models;
                        var parsingResult = result.Result;
                        
                        if (parsingResult != Core.Enums.ConversionResult.Success)
                        {
                            Console.WriteLine($"Failed to parse input file: {parsingResult}");
                            context.ExitCode = 1;
                            return;
                        }
                        
                        // Generate output file
                        var fileGenerator = new RevitFileGenerator(progressReporter);
                        var genResult = await fileGenerator.GenerateFileAsync(
                            models, options, progressReporter);
                        
                        string filePath = genResult.FilePath;
                        var generationResult = genResult.Result;
                        
                        if (generationResult != Core.Enums.ConversionResult.Success)
                        {
                            Console.WriteLine($"Failed to generate output file: {generationResult}");
                            context.ExitCode = 1;
                            return;
                        }
                        
                        Console.WriteLine($"Conversion successful. Output file: {filePath}");
                        context.ExitCode = 0;
                    }
                    else
                    {
                        // Handle other file formats here by creating appropriate converters
                        Console.WriteLine("Unsupported input file format.");
                        context.ExitCode = 1;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during conversion: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    context.ExitCode = 1;
                }
            });
            
            // Parse the command line
            return await rootCommand.InvokeAsync(args);
        }
    }
}