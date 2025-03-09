using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerator;

CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

var syntaxTree1 = CSharpSyntaxTree.ParseText("""
    public partial class A {}
    """, parseOptions);

var refs = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(v => !v.IsDynamic)
    .Where(v => v.Location.Contains("Microsoft.NETCore.App"))
    .Where(v => File.Exists(v.Location))
    .Select(v => MetadataReference.CreateFromFile(v.Location));

var compilation1 = CSharpCompilation.Create("SGTests", [syntaxTree1], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

var driver1 = CSharpGeneratorDriver.Create(new IncrementalGenerator());

CancellationTokenSource cts = new CancellationTokenSource();

var driver2 = driver1.RunGeneratorsAndUpdateCompilation(compilation1, out var outputCompilation1, out _, cts.Token);

var syntaxTree2 = CSharpSyntaxTree.ParseText("""
    public partial class B {}
    """, parseOptions);

var compilation2 = compilation1.AddSyntaxTrees(syntaxTree2);

var driver3 = driver2.RunGeneratorsAndUpdateCompilation(compilation2, out var outputCompilation2, out _);


;