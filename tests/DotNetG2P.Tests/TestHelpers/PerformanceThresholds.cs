using System;

namespace DotNetG2P.Tests.TestHelpers
{
    internal static class PerformanceThresholds
    {
        private static readonly bool s_strictMode =
            string.Equals(Environment.GetEnvironmentVariable("DOTNETG2P_STRICT_PERF"), "1", StringComparison.Ordinal);

        public static long Milliseconds(long strictThreshold, long relaxedThreshold)
        {
            return s_strictMode ? strictThreshold : relaxedThreshold;
        }

        public static double Megabytes(double strictThreshold, double relaxedThreshold)
        {
            return s_strictMode ? strictThreshold : relaxedThreshold;
        }
    }
}
