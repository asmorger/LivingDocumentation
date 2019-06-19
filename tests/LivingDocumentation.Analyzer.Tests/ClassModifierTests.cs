using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace LivingDocumentation.Analyzer.Tests
{
    [TestClass]
    public class ClassModifierTests
    {
        [TestMethod]
        public void ClassWithoutModifier_Should_HaveDefaultInternalModifier()
        {
            // Assign
            var source = @"
            class Test
            {
            }
            ";

            // Act
            var types = VisitSyntaxTree(source);

            // Assert
            types.First().Modifiers.Should().HaveCount(1);
            types.First().Modifiers.Should().Contain("internal");
        }

        [TestMethod]
        public void PublicClass_Should_HavePublicModifier()
        {
            // Assign
            var source = @"
            public class Test
            {
            }
            ";

            // Act
            var types = VisitSyntaxTree(source);

            // Assert
            types.First().Modifiers.Should().HaveCount(1);
            types.First().Modifiers.Should().Contain("public");
        }

        [TestMethod]
        public void StaticClass_Should_HaveStaticModifier()
        {
            // Assign
            var source = @"
            static class Test
            {
            }
            ";

            // Act
            var types = VisitSyntaxTree(source);

            // Assert
            types.First().Modifiers.Should().HaveCountGreaterThan(1);
            types.First().Modifiers.Should().Contain("static");
        }

        [TestMethod]
        public void NestedClassWithoutModifier_Should_HaveDefaultPrivateModifier()
        {
            // Assign
            var source = @"
            class Test
            {
                class NestedTest
                {
                }
            }
            ";

            // Act
            var types = VisitSyntaxTree(source);

            // Assert
            types.Last().Modifiers.Should().HaveCount(1);
            types.Last().Modifiers.Should().Contain("private");
        }

        [TestMethod]
        public void NestedPublicClass_Should_HavePublicModifier()
        {
            // Assign
            var source = @"
            class Test
            {
                public class NestedTest
                {
                }
            }
            ";

            // Act
            var types = VisitSyntaxTree(source);

            // Assert
            types.Last().Modifiers.Should().HaveCount(1);
            types.Last().Modifiers.Should().Contain("public");
        }

        private static IReadOnlyList<TypeDescription> VisitSyntaxTree(string source)
        {
            source.Should().NotBeNull();

            var syntaxTree = CSharpSyntaxTree.ParseText(source.Trim());
            var compilation = CSharpCompilation.Create("Test")
                                               .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                               .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                               .AddSyntaxTrees(syntaxTree);

            var diagnostics = compilation.GetDiagnostics();
            diagnostics.Should().HaveCount(0);

            var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

            var types = new List<TypeDescription>();

            var visitor = new SourceAnalyzer(semanticModel, types, Enumerable.Empty<AssemblyIdentity>().ToList());
            visitor.Visit(syntaxTree.GetRoot());

            return types;
        }
    }
}
