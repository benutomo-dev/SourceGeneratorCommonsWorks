namespace SandBoxLib1;

public class Class1
{

}

partial class OuterA
{
    readonly ref partial struct InnerB
    {
        //readonly ref int _a;

        //public readonly ref readonly int MethodA()
        //{
        //    return ref _a;
        //}

        //public readonly partial int MethodB();

        //public readonly partial int? MethodC();
    }

    ref partial struct InnerC
    {
        //ref int _a;

        //public ref int MethodA()
        //{
        //    return ref _a;
        //}

        public int X()
        {
            return 0;
        }
    }

}
class OuterB : IEquatable<OuterB.X>
{
    bool IEquatable<X>.Equals(X? other)
    {
        _ = new SandBoxLib1.Class1();
        throw new NotImplementedException();
    }

    class X
    {

    }
}
