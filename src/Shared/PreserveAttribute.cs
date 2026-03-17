using System;

namespace DotNetG2P.Internal
{
    /// <summary>
    /// Unity IL2CPP linker が認識する Preserve 属性。
    /// クラスが AOT strip されないようにマークする。
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Method | AttributeTargets.Constructor,
        AllowMultiple = false)]
    internal sealed class PreserveAttribute : Attribute { }
}
