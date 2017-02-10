using System;
using System.Linq;
using EFSecondLevelCache.Core.NET46Sample.DataLayer;
using EFSecondLevelCache.Core.NET46Sample.DataLayer.Entities;

namespace EFSecondLevelCache.Core.NET46Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new SampleContext())
            {
                context.Posts.Add(new Post { Title = "Title 1" });
                context.SaveChanges();

                var posts = context.Posts.Cacheable().ToList();
                Console.WriteLine(posts.First().Title);
            }
        }
    }
}