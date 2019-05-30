using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;

namespace EFSecondLevelCache.Core.PerformanceTests
{
    public class BenchmarkTests
    {
        private int _count;

        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine("SetupDatabase");
            SetupDatabase();
        }

        private static void SetupDatabase()
        {
            using (var serviceScope = TestsServiceProvider.WithJsonSerializerInstance.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    if (db.Database.EnsureCreated())
                    {
                        var student1 = new Student { Name = "user 1" };
                        db.Students.Add(student1);

                        var student2 = new Student { Name = "user 2" };
                        db.Students.Add(student2);

                        var student3 = new Student { Name = "user 3" };
                        db.Students.Add(student3);

                        db.SaveChanges();
                    }
                }
            }
        }

        [Benchmark(Baseline = true)]
        public void RunQueryDirectly()
        {
            using (var serviceScope = TestsServiceProvider.WithJsonSerializerInstance.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var students = db.Students.Where(x => x.Id > 0).ToList();
                    _count = students.Count;
                }
            }
        }

        [Benchmark]
        public void RunCacheableQueryWithJsonSerializer()
        {
            using (var serviceScope = TestsServiceProvider.WithJsonSerializerInstance.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var students = db.Students.Where(x => x.Id > 0).Cacheable().ToList();
                    _count = students.Count;
                }
            }
        }

        [Benchmark]
        public void RunCacheableQueryWithGzJsonSerializer()
        {
            using (var serviceScope = TestsServiceProvider.WithGzJsonSerializerInstance.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var students = db.Students.Where(x => x.Id > 0).Cacheable().ToList();
                    _count = students.Count;
                }
            }
        }

        [Benchmark]
        public void RunCacheableQueryWithDictionaryHandle()
        {
            using (var serviceScope = TestsServiceProvider.WithDictionaryHandleInstance.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var students = db.Students.Where(x => x.Id > 0).Cacheable().ToList();
                    _count = students.Count;
                }
            }
        }

        [Benchmark]
        public void RunCacheableQueryWithMicrosoftMemoryCache()
        {
            using (var serviceScope = TestsServiceProvider.WithMicrosoftMemoryCacheInstance.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var db = serviceScope.ServiceProvider.GetRequiredService<SampleContext>())
                {
                    var students = db.Students.Where(x => x.Id > 0).Cacheable().ToList();
                    _count = students.Count;
                }
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            Console.WriteLine($"_count: {_count}");
        }
    }
}