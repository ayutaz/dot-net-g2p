// Copyright (c) ayutaz. Licensed under the Apache-2.0 License.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DotNetG2P.Internal
{
    /// <summary>
    /// ArrayPool-backed ref struct for zero-allocation string building.
    /// Based on the ZString / .NET runtime ValueStringBuilder pattern.
    /// </summary>
    internal ref struct ValueStringBuilder
    {
        private char[] _arrayToReturnToPool;
        private Span<char> _chars;
        private int _pos;

        public ValueStringBuilder(int initialCapacity)
        {
            _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            _chars = _arrayToReturnToPool;
            _pos = 0;
        }

        public int Length => _pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c)
        {
            if ((uint)_pos < (uint)_chars.Length)
            {
                _chars[_pos++] = c;
            }
            else
            {
                GrowAndAppend(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(string? s)
        {
            if (s == null) return;

            int pos = _pos;
            if (pos + s.Length <= _chars.Length)
            {
                s.AsSpan().CopyTo(_chars.Slice(pos));
                _pos = pos + s.Length;
            }
            else
            {
                AppendSlow(s.AsSpan());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(ReadOnlySpan<char> value)
        {
            int pos = _pos;
            if (pos + value.Length <= _chars.Length)
            {
                value.CopyTo(_chars.Slice(pos));
                _pos = pos + value.Length;
            }
            else
            {
                AppendSlow(value);
            }
        }

        public void Append(int value)
        {
            // int.TryFormat is available in .NET Standard 2.1
            if (value.TryFormat(_chars.Slice(_pos), out int charsWritten))
            {
                _pos += charsWritten;
            }
            else
            {
                Grow(_pos + 16);
                if (!value.TryFormat(_chars.Slice(_pos), out charsWritten))
                {
                    ThrowHelper.ThrowInvalidOperation("Failed to format integer");
                }
                _pos += charsWritten;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AppendSlow(ReadOnlySpan<char> value)
        {
            Grow(_pos + value.Length);
            value.CopyTo(_chars.Slice(_pos));
            _pos += value.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppend(char c)
        {
            Grow(_pos + 1);
            _chars[_pos++] = c;
        }

        private void Grow(int requiredMinCapacity)
        {
            int newCapacity = Math.Max(requiredMinCapacity, _chars.Length * 2);
            char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);
            _chars.Slice(0, _pos).CopyTo(newArray);

            char[] toReturn = _arrayToReturnToPool;
            _arrayToReturnToPool = newArray;
            _chars = newArray;

            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn, clearArray: false);
            }
        }

        public override string ToString()
        {
            string result = _chars.Slice(0, _pos).ToString();
            return result;
        }

        /// <summary>
        /// Returns the string and disposes the builder in one operation.
        /// </summary>
        public string ToStringAndDispose()
        {
            string result = _chars.Slice(0, _pos).ToString();
            Dispose();
            return result;
        }

        public void Dispose()
        {
            char[]? toReturn = _arrayToReturnToPool;
            _arrayToReturnToPool = null!;
            _chars = default;
            _pos = 0;
            if (toReturn != null)
            {
                ArrayPool<char>.Shared.Return(toReturn, clearArray: false);
            }
        }
    }
}
