using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace SourceGeneratorCommons.Tests;


public class IsAssignableToTests
{
    static SyntaxTree syntaxTree;
    static CSharpCompilation compilation;
    static SemanticModel sematicModel;

    static IsAssignableToTests()
    {
        (compilation, syntaxTree) = UnitTestCompiler.Compile("""
            #nullable disable
            namespace Ns
            {
                public class Class1 { }

                public class Class2 { }

                public struct Struct1 { }

                public struct Struct2 { }

                public ref struct RefStruct1 { }

                public ref struct RefStruct2 { }

                public interface Interface1 { }

                public interface Interface2 { }

                public class SubClassOfClass1 : Class1 { }

                public class ClassT1<T> { }

                public interface DerivedInterface1 : Interface1 { }

                public interface MixInInterface1 : Interface1, Interface2 { }

                public interface InterfaceT1<T> { }

                public interface InterfaceInT1<in T> { }

                public interface InterfaceOutT1<out T> { }

                public interface InterfaceTWhereClass1<T> where T : class { }

                public interface InterfaceTWhereStruct1<T> where T : struct { }

                public interface InterfaceTWhereInterface1<T> where T : Interface1 { }

                public ref struct Assigns
                {
                    object object1;
                    Class1 class1;
                    Class2 class2;
                    Struct1 struct1;
                    Struct2 struct2;
                    RefStruct1 refStruct1;
                    RefStruct2 refStruct2;
                    SubClassOfClass1 subClassOfClass1;
                    ClassT1<Class1> classClass1T1;
                    ClassT1<Struct1> classStructT1;
                    Interface1 interface1;
                    Interface2 interface2;
                    DerivedInterface1 derivedInterface1;
                    MixInInterface1 mixInInterface1;
                    InterfaceT1<Struct1> interfaceStruct1T1;
                    InterfaceT1<Class1> interfaceClass1T1;
                    InterfaceInT1<Struct1> interfaceInStruct1T1;
                    InterfaceInT1<Class1> interfaceInClass1T1;
                    InterfaceInT1<SubClassOfClass1> interfaceInSubClassOfClass1T1;
                    InterfaceOutT1<Struct1> interfaceOutStruct1T1;
                    InterfaceOutT1<Class1> interfaceOutClass1T1;
                    InterfaceOutT1<SubClassOfClass1> interfaceOutSubClassOfClass1T1;
                    InterfaceTWhereClass1<Class1> interfaceClass1TWhereClass1;
                    InterfaceTWhereStruct1<Struct1> interfaceStruct1TWhereStruct1;
                    InterfaceTWhereInterface1<Interface1> interfaceInterface1TWhereInterface1;

                    public void ValidImplicitAssigns1<T>()
                    {
                        InterfaceT1<T> interfaceT1;
                        InterfaceInT1<T> interfaceInT1;
                        InterfaceOutT1<T> interfaceOutT1;

                        object1 = class1;
                        object1 = struct1;
                        object1 = subClassOfClass1;
                        object1 = mixInInterface1;
                        object1 = interfaceStruct1T1;
                        object1 = interfaceInClass1T1;
                        object1 = default(InterfaceT1<T>);

                        interfaceInSubClassOfClass1T1 = default(InterfaceInT1<Class1>);
                        interfaceInSubClassOfClass1T1 = default(InterfaceInT1<SubClassOfClass1>);
                        interfaceOutClass1T1 = default(InterfaceOutT1<Class1>);
                        interfaceOutClass1T1 = default(InterfaceOutT1<SubClassOfClass1>);

                        class1 = default(Class1);

                        interfaceT1 = default(InterfaceT1<T>);
                        interfaceInT1 = default(InterfaceInT1<T>);
                        interfaceOutT1 = default(InterfaceOutT1<T>);
                    }

                    public void ValidImplicitAssigns2<T>() where T : Class1
                    {
                        InterfaceT1<T> interfaceT1;
                        InterfaceInT1<T> interfaceInT1;
                        InterfaceOutT1<T> interfaceOutT1;
                        InterfaceTWhereClass1<T> interfaceTWhereClass1;

                        object1 = default(T);
                        class1 = default(T);

                        interfaceT1 = default(InterfaceT1<T>);
                        interfaceInT1 = default(InterfaceInT1<T>);
                        interfaceOutT1 = default(InterfaceOutT1<T>);
                        interfaceTWhereClass1 = default(InterfaceTWhereClass1<T>);

                        interfaceInT1 = default(InterfaceInT1<Class1>);
                    }

                    public void ValidImplicitAssigns3<T>() where T : SubClassOfClass1
                    {
                        object1 = default(T);
                        class1 = default(T);
                    }

                    public void InvalidAssigns1<T>()
                    {
                        object1 = refStruct1;
                        class1 = struct1;
                        class1 = interface1;
                        struct1 = class1;
                        struct1 = interface1;
                        interface1 = class1;
                        interface1 = struct1;

                        interfaceInClass1T1 = default(InterfaceInT1<SubClassOfClass1>);
                        interfaceOutSubClassOfClass1T1 = default(InterfaceOutT1<Class1>);
                    }
                }
            }
            #nullable restore
            """);

        sematicModel = compilation.GetSemanticModel(syntaxTree, true);
    }

    public static TheoryData<int, int, string> GetAssignExpressinSpans()
    {
        var theoryData = new TheoryData<int, int, string>();

        foreach (var expressionSyntax in syntaxTree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            var span = expressionSyntax.Span;
            theoryData.Add(span.Start, span.Length, expressionSyntax.ToString().Trim());
        }

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetAssignExpressinSpans))]
    public void IsAssignableTo(int start, int length, string code)
    {
        GC.KeepAlive(code);

        var span = new TextSpan(start, length);

        var syntaxNode = (AssignmentExpressionSyntax)syntaxTree.GetRoot().FindNode(span);

        var x = syntaxTree.GetDiagnostics(syntaxNode);

        var diagnostics = compilation.GetDiagnostics().Where(v => span.Contains(v.Location.SourceSpan)).ToArray();

        var isAssignableAtCompileTime = !diagnostics.Any(v => v.Id is "CS0029" or "CS0266");

        var destination = sematicModel.GetTypeInfo(syntaxNode.Left);
        var source = sematicModel.GetTypeInfo(syntaxNode.Right);

        var assignmentOperation = (IAssignmentOperation)sematicModel.GetOperation(syntaxNode)!;

        var isAssignable = source.Type.IsAssignableTo(destination.Type, compilation);

        Assert.Equal(isAssignableAtCompileTime, isAssignable);
    }
}