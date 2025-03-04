using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorCommons;

namespace SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class IncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context =>
        {
            context.AddSource("PostInitializationOutput.cs", """
                // this is sample.
                """);
        });

        static bool predicate(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is not TypeDeclarationSyntax typeDeclarationSyntax)
                return false;

            return typeDeclarationSyntax.Modifiers.Any(v => v.Text == "partial");
        }

        static TypeDefinitionInfo? transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

            var typeSymbol = (ITypeSymbol?)context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax, cancellationToken);

            return typeSymbol?.BuildTypeDefinitionInfo().DefinitionInfo;
        }

        var source = context.SyntaxProvider
            .CreateSyntaxProvider(predicate, transform)
            .Collect()
            .SelectMany((typeDefinitionInfoList, cancellationToken) =>
            {
                return typeDefinitionInfoList.Where(v => v is not null).Select(v => v!).Distinct();
            });

        context.RegisterSourceOutput(source, (context, source) =>
        {
            var hintName = $"{source.MakeStandardHintName()}.cs";

            var builder = new SourceBuilder(context, hintName);

            using (builder.BeginTypeDefinitionBlock(source, $" // This is source generated code."))
            {
            }
                
            builder.Commit();
        });
    }
}
