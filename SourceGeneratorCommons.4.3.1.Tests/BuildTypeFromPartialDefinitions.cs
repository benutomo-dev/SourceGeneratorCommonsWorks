using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ShouldMethodAssertion.ShouldExtensions;
using Xunit.Abstractions;

namespace SourceGeneratorCommons.Tests;
public class BuildTypeFromPartialDefinitions(ITestOutputHelper Output)
{
    [Generator(LanguageNames.CSharp)]
    private class IncrementalGenerator(string metedataName) : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var typeProvider = context.CreateCsDeclarationProvider()
                .Select((v, _) =>
                {
                    var type = v.GetTypeReferenceByMetadataName(metedataName)!;

                    var methods = v.Compilation.GetTypeByMetadataName(metedataName)!
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(v => v.IsPartialDefinition)
                        .Select(methodSymbol => v.GetMethodDeclaration(methodSymbol));

                    return (type, methods);
                });

            context.RegisterSourceOutput(typeProvider, (context, args) =>
            {
                using var sb = new SourceBuilder(context, $"{args.type.Cref}.cs");
                using (sb.BeginTypeDefinitionBlock(args.type.TypeDefinition))
                {
                    foreach (var method in args.methods)
                    {
                        using (sb.BeginMethodDefinitionBlock(method))
                        {
                            if (!method.IsVoidLikeMethod)
                            {
                                sb.AppendLineWithFirstIndent($"return default!;");
                            }
                        }
                    }
                }
                sb.Commit();
            });
        }
    }

    private static Dictionary<string, (string metedataName, string source)> s_testData = new()
    {
        [@"global::MinClass"] = (
            "MinClass",
            """
            partial class MinClass
            {
                public static partial MinClass Method(MinClass? nullable);
            }
            """),
        [@"NameSpace.MinClass"] = (
            "NameSpace.MinClass",
            """
            namespace NameSpace
            {
                partial class MinClass
                {
                    public static partial MinClass Method(MinClass? nullable);
                }
            }
            """),
        [@"OuterNameSpace.InnerNameSpace.ContainerClass+InnerClass"] = (
            "OuterNameSpace.InnerNameSpace.ContainerClass+InnerClass",
            """
            namespace OuterNameSpace
            {
                namespace InnerNameSpace
                {
                    partial class ContainerClass
                    {
                        partial class InnerClass
                        {
                            public static partial InnerClass Method(InnerClass? nullable);
                        }
                    }
                }
            }
            """),
        [@"OuterNameSpace.InnerNameSpace.ContainerClass<TOuter1, TOuter2>+InnerClass<TInner1, TInner2>"] = (
            "OuterNameSpace.InnerNameSpace.ContainerClass`2+InnerClass`2",
            """
            namespace OuterNameSpace
            {
                namespace InnerNameSpace
                {
                    partial class ContainerClass<TOuter1, TOuter2>
                        where TOuter1 : struct, global::System.IComparable<TOuter1>
                    {
                        partial class InnerClass<TInner1, TInner2>
                            where TInner2 : class, global::System.IComparable<TInner2>, global::System.Collections.Generic.IEnumerable<TOuter1>
                        {
                        }
                    }
                }
            }
            """),
        [@"OuterNameSpace.InnerNameSpace.ContainerClass<TOuter1, TOuter2>"] = (
            "OuterNameSpace.InnerNameSpace.ContainerClass`2",
            """
            namespace OuterNameSpace
            {
                namespace InnerNameSpace
                {
                    partial class ContainerClass<TOuter1, TOuter2>
                        where TOuter1 : struct, global::System.IComparable<TOuter1>
                    {
                        class InnerClass<TInner1, TInner2>
                            where TInner2 : class, global::System.IComparable<TInner2>, global::System.Collections.Generic.IEnumerable<TOuter1>
                        {
                        }
            
                        private static partial InnerClass<TInner1, TInner2> Method<TInner1, TInner2>(InnerClass<TInner1, TInner2>? nullable)
                            where TInner2 : class, global::System.IComparable<TInner2>, global::System.Collections.Generic.IEnumerable<TOuter1>;
                    }
                }
            }
            """),
    };

    public static TheoryData<string> GetTestDataKeys()
    {
        var theoryData = new TheoryData<string>();
        s_testData.Keys.ForEach(theoryData.Add);
        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetTestDataKeys))]
    public void RunTest(string testDataName)
    {
        var testData = s_testData[testDataName];

        Output.WriteLine($"<InputSource>");
        Output.WriteLine(testData.source);
        Output.WriteLine($"");
        Output.WriteLine($"");

        var syntaxTree = CSharpSyntaxTree.ParseText(testData.source, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        var compilation = UnitTestCompiler.Default.AddSyntaxTrees(syntaxTree);

        var generatorDriver = CSharpGeneratorDriver.Create(new IncrementalGenerator(testData.metedataName));

        generatorDriver = (CSharpGeneratorDriver)generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        foreach (var generatedTree in generatorDriver.GetRunResult().GeneratedTrees)
        {
            Output.WriteLine($"<GeneratedSource:{generatedTree.FilePath}>");
            Output.WriteLine(generatedTree.GetText().ToString());
            Output.WriteLine($"");
            Output.WriteLine($"");
        }

        outputCompilation.GetDiagnostics().Should().BeEmpty();
    }
}
