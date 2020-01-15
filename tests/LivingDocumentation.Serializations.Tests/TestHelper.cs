﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LivingDocumentation.Analyzer.Tests
{
    internal class TestHelper
    {
        public static IReadOnlyList<TypeDescription> VisitSyntaxTree(string source)
        {
            source.Should().NotBeNullOrWhiteSpace("without source code there is nothing to test");

            var syntaxTree = CSharpSyntaxTree.ParseText(source.Trim());
            var compilation = CSharpCompilation.Create("Test")
                                               .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                               .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                               .AddSyntaxTrees(syntaxTree);

            var diagnostics = compilation.GetDiagnostics();
            diagnostics.Should().HaveCount(0, "there shoudn't be any compile errors");

            var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

            var types = new List<TypeDescription>();

            var visitor = new SourceAnalyzer(semanticModel, types);
            visitor.Visit(syntaxTree.GetRoot());

            return types;
        }
    }
}
