﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using CommandLine;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace LivingDocumentation
{
    public class Program
    {
        private static ParserResult<Options> ParsedResults;

        public class Options
        {
            [Option("solution", Required = true, HelpText = "The solution to analyze.")]
            public string SolutionPath { get; set; }

            [Option("output", Required = true, HelpText = "The location of the output.")]
            public string OutputPath { get; set; }

            [Option('v', "verbose", Default = false, HelpText = "Show warnings during compilation.")]
            public bool VerboseOutput { get; set; }
        }

        public static async Task Main(string[] args)
        {
            ParsedResults = Parser.Default.ParseArguments<Options>(args);

            await ParsedResults.MapResult(
                options => RunApplicationAsync(options),
                errors => Task.FromResult(1)
            );
        }

        private static async Task RunApplicationAsync(Options options)
        {
            var types = new List<TypeDescription>();

            await AnalyzeSolutionAsync(types, options.SolutionPath, options.VerboseOutput);

            // Write analysis 
            var serializerSettings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                ContractResolver = new SkipEmptyCollectionsContractResolver(),
                TypeNameHandling = TypeNameHandling.Auto
            };

            var result = JsonConvert.SerializeObject(types.OrderBy(t => t.FullName), serializerSettings);

            await File.WriteAllTextAsync(options.OutputPath, result);
            Console.WriteLine($"Living Documentation Analysis output generated {options.OutputPath}");
        }

        private static async Task AnalyzeSolutionAsync(IList<TypeDescription> types, string solutionFile, bool verboseOutput)
        {
            var manager = new AnalyzerManager(solutionFile);
            var workspace = manager.GetWorkspace();
            var assembliesInSolution = workspace.CurrentSolution.Projects.Select(p => p.AssemblyName).ToList();

            // Every project in the solution, except unit test projects
            var projects = workspace.CurrentSolution.Projects
                .Where(p => !manager.Projects.First(mp => p.Id.Id == mp.Value.ProjectGuid).Value.ProjectFile.PackageReferences.Any(pr => pr.Name.Contains("Test")));

            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync();
                var referencedAssemblies = compilation.ReferencedAssemblyNames.Where(a => !assembliesInSolution.Contains(a.Name)).ToList();

                if (verboseOutput)
                {
                    var diagnostics = compilation.GetDiagnostics();
                    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        Console.WriteLine($"The following errors occured during compilation of project '{project.FilePath}'");
                        foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                        {
                            Console.WriteLine("- " + diagnostic.ToString());
                        }
                    }
                }

                // Every file in the project
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

                    var visitor = new SourceAnalyzer(semanticModel, types, referencedAssemblies);
                    visitor.Visit(syntaxTree.GetRoot());
                }
            }

            workspace.Dispose();
        }
    }
}