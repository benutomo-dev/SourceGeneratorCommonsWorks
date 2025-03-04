namespace SourceGeneratorCommons.Tests;

public class BuildTypeDefinitionInfoTests
{
    [Fact]
    public void X()
    {
        var compilation = UnitTestCompiler.Default;

        var openGenericEnumerableTypeSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");

        Assert.NotNull(openGenericEnumerableTypeSymbol);

        {
            var definitionWithReference = openGenericEnumerableTypeSymbol.BuildTypeDefinitionInfo();

            //Assert.Null(definitionWithReference.ReferenceInfo);

            Assert.Equal("IEnumerable", definitionWithReference.DefinitionInfo.Name);
            Assert.Equal("System.Collections.Generic.IEnumerable<T>", definitionWithReference.DefinitionInfo.FullName);
        }

        var intTypeSymbol = compilation.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32);

        var openGenericLazyTypeSymbol = compilation.GetTypeByMetadataName("System.Lazy`1");

        Assert.NotNull(openGenericLazyTypeSymbol);

        var nullableLazyIntTypeSymbol = openGenericLazyTypeSymbol
            .Construct(intTypeSymbol)
            .WithNullableAnnotation(Microsoft.CodeAnalysis.NullableAnnotation.Annotated);

        var nullableLazyIntGenericEnumerableTypeSymbol = openGenericEnumerableTypeSymbol
            .Construct(nullableLazyIntTypeSymbol)
            .WithNullableAnnotation(Microsoft.CodeAnalysis.NullableAnnotation.Annotated);

        {
            var definitionWithReference = nullableLazyIntGenericEnumerableTypeSymbol.BuildTypeDefinitionInfo();

            Assert.Equal("IEnumerable", definitionWithReference.DefinitionInfo.Name);
            Assert.Equal("System.Collections.Generic.IEnumerable<T>", definitionWithReference.DefinitionInfo.FullName);

            Assert.NotNull(definitionWithReference.ReferenceInfo);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Lazy<System.Int32>?>?", definitionWithReference.ReferenceInfo.ToString());
            Assert.True(definitionWithReference.ReferenceInfo.IsNullableAnnotated);
            Assert.Single(definitionWithReference.ReferenceInfo.TypeArgs);
            Assert.Single(definitionWithReference.ReferenceInfo.TypeArgs[0]);

            var lazyReference = definitionWithReference.ReferenceInfo.TypeArgs[0][0];

            Assert.Equal("Lazy", lazyReference.TypeDefinition.Name);
            Assert.True(lazyReference.IsNullableAnnotated);
            Assert.Single(lazyReference.TypeArgs);
            Assert.Single(lazyReference.TypeArgs[0]);

            var intReference = lazyReference.TypeArgs[0][0];

            Assert.Equal("Int32", intReference.TypeDefinition.Name);
            Assert.False(intReference.IsNullableAnnotated);
            Assert.Empty(intReference.TypeArgs);

        }
    }
}
