#if !UNITY_5_3_OR_NEWER
using System;

namespace UnityEngine.Scripting
{
    /// <summary>
    /// Unity IL2CPP linker が認識する UnityEngine.Scripting.PreserveAttribute 互換属性。
    /// Unity 環境では IL2CPP リンカーがこの名前空間を認識し、AOT strip を防止する。
    /// 非 Unity 環境（.NET）ではマーカーとして機能する。
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct |
        AttributeTargets.Method | AttributeTargets.Constructor,
        AllowMultiple = false)]
    internal sealed class PreserveAttribute : Attribute { }
}
#endif
