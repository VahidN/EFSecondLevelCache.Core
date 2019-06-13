using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace EFSecondLevelCache.Core.PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance)
                    .With(BenchmarkDotNet.Analysers.EnvironmentAnalyser.Default)
                    .With(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub)
                    .With(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default)
                    .With(StatisticColumn.Mean)
                    .With(StatisticColumn.Median)
                    .With(StatisticColumn.StdDev)
                    .With(StatisticColumn.OperationsPerSecond)
                    .With(BaselineRatioColumn.RatioMean)
                    .With(RankColumn.Arabic)
                    .With(Job.Core
                        .WithIterationCount(10)
                        .WithInvocationCount(16)
                        .WithIterationTime(TimeInterval.FromSeconds(10))
                        .WithWarmupCount(4)
                        .WithLaunchCount(1));
            BenchmarkRunner.Run<BenchmarkTests>(config);
        }
    }
}