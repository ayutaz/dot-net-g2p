// Copyright (c) ayutaz. Licensed under the Apache-2.0 License.

using System;
using System.Runtime.CompilerServices;

namespace DotNetG2P.Internal
{
    /// <summary>
    /// Centralized exception throw helpers with [NoInlining] to keep hot paths small.
    /// </summary>
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange(string paramName)
            => throw new ArgumentOutOfRangeException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRange(string paramName, string message)
            => throw new ArgumentOutOfRangeException(paramName, message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNull(string paramName)
            => throw new ArgumentNullException(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowObjectDisposed(string objectName)
            => throw new ObjectDisposedException(objectName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperation(string message)
            => throw new InvalidOperationException(message);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidData(string message)
            => throw new System.IO.InvalidDataException(message);
    }
}
