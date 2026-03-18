using System;
using System.Collections.Generic;

namespace DotNetG2P.Internal
{
    internal static class BatchConversionHelper
    {
        public static TResult[] ConvertToArray<TResult>(IReadOnlyList<string> texts, Func<string, TResult> converter)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            if (texts.Count == 0)
                return Array.Empty<TResult>();

            var results = new TResult[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = converter(texts[i]);

            return results;
        }

        public static List<TResult> ConvertToList<TResult>(IReadOnlyList<string> texts, Func<string, TResult> converter)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            var results = new List<TResult>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(converter(texts[i]));

            return results;
        }

        public static TResult[] ConvertToArray<TContext, TState, TResult>(
            IReadOnlyList<string> texts,
            TContext context,
            TState state,
            Func<TContext, string, TState, TResult> converter)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            if (texts.Count == 0)
                return Array.Empty<TResult>();

            var results = new TResult[texts.Count];
            for (var i = 0; i < texts.Count; i++)
                results[i] = converter(context, texts[i], state);

            return results;
        }

        public static List<TResult> ConvertToList<TContext, TState, TResult>(
            IReadOnlyList<string> texts,
            TContext context,
            TState state,
            Func<TContext, string, TState, TResult> converter)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));
            if (converter == null) throw new ArgumentNullException(nameof(converter));

            var results = new List<TResult>(texts.Count);
            for (var i = 0; i < texts.Count; i++)
                results.Add(converter(context, texts[i], state));

            return results;
        }
    }
}
