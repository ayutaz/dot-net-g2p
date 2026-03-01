using System;
using System.Collections.Generic;

namespace DotNetG2P
{
    /// <summary>
    /// 形態素解析器のインターフェース。
    /// テキストをトークン列に分割する。
    /// </summary>
    /// <remarks>
    /// 実装はスレッドセーフとは限らない。
    /// 複数スレッドから同時にアクセスする場合は、呼び出し側で排他制御を行うこと。
    /// </remarks>
    public interface ITokenizer : IDisposable
    {
        /// <summary>
        /// テキストを形態素解析し、トークン列を返す。
        /// </summary>
        IReadOnlyList<IToken> Tokenize(string text);
    }
}
