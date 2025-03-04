using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

var syntaxTree = CSharpSyntaxTree.ParseText("""

    """, parseOptions);

var refs = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(v => !v.IsDynamic)
    .Where(v => v.Location.Contains("Microsoft.NETCore.App"))
    .Where(v => File.Exists(v.Location))
    .Select(v => MetadataReference.CreateFromFile(v.Location));

var compilation = CSharpCompilation.Create("SGTests", [syntaxTree], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

var sematicModel = compilation.GetSemanticModel(syntaxTree, true);

var objectType = compilation.GetSpecialType(SpecialType.System_Object);
var enumerableType = (INamedTypeSymbol)compilation.GetTypeByMetadataName(@"System.Collections.Generic.IEnumerable`1")!;

var enumerableType2 = enumerableType.Construct(objectType);

var enumerableType3 = enumerableType.ConstructUnboundGenericType();
;