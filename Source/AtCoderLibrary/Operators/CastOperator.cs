using System.Runtime.CompilerServices;

namespace AtCoder.Operators
{
#if GENERIC_MATH
    [System.Obsolete(Internal.Constants.UseGenericMath)]
#endif
    [IsOperator]
    public interface ICastOperator<in TFrom, out TTo>
    {
        TTo Cast(TFrom y);
    }

#if GENERIC_MATH
    [System.Obsolete(Internal.Constants.UseGenericMath)]
#endif
    public struct SameTypeCastOperator<T> : ICastOperator<T, T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Cast(T y) => y;
    }

#if GENERIC_MATH
    [System.Obsolete(Internal.Constants.UseGenericMath)]
#endif
    public struct IntToLongCastOperator : ICastOperator<int, long>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Cast(int y) => y;
    }
}
