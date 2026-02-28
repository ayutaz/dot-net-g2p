using System;
using System.Collections.Generic;

namespace DotNetG2P
{
    /// <summary>
    /// 形態素解析器のインターフェース。
    /// テキストをトークン列に分割する。
    /// </summary>
    public interface ITokenizer : IDisposable
    {
        /// <summary>
        /// テキストを形態素解析し、トークン列を返す。
        /// </summary>
        IReadOnlyList<IToken> Tokenize(string text);
    }
}
