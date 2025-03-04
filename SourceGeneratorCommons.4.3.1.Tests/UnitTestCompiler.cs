using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SourceGeneratorCommons.Tests;

internal static class UnitTestCompiler
{
    public static CSharpCompilation Default { get; }

    static UnitTestCompiler()
    {
        var refs = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(v => !v.IsDynamic)
            .Where(v => v.Location.Contains("Microsoft.NETCore.App"))
            .Where(v => File.Exists(v.Location))
            .Select(v => MetadataReference.CreateFromFile(v.Location));

        Default = CSharpCompilation.Create("SGTests", [], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    public static (CSharpCompilation Compilation, SyntaxTree SyntaxTree) Compile(string source)
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = Default.AddSyntaxTrees(syntaxTree);

        return (compilation, syntaxTree);
    }
}
