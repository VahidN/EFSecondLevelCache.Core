using System;
using System.Linq;
using System.Threading.Tasks;
using EFSecondLevelCache.Core;
using Microsoft.EntityFrameworkCore;

namespace Issue15
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SetupDatabase();

            var serviceProvider = ConfigureServices.Instance;

            using (var context = new SampleContext())
            {

                Console.WriteLine($"1,en");
                /*
                SELECT TOP(2) [u].[Id], [u].[IdPaymentType], [u].[Language]
                FROM [Payments] AS [u]
                WHERE ([u].[IdPaymentType] = 1) AND ([u].[Language] = N'en')
                */
                var item = await context.Payments
                  .Where(u => u.IdPaymentType == 1 && u.Language.Equals("en"))
                  .Cacheable(serviceProvider)
                  .SingleOrDefaultAsync();
                Console.WriteLine($"1,en -> item.Id= {item.Id}"); // --> 1

                Console.WriteLine($"{Environment.NewLine}2,en");
                /*
                SELECT TOP(2) [u].[Id], [u].[IdPaymentType], [u].[Language]
                FROM [Payments] AS [u]
                WHERE ([u].[IdPaymentType] = 2) AND ([u].[Language] = N'en')
                */
                item = await context.Payments
                  .Where(u => u.IdPaymentType == 2 && u.Language.Equals("en"))
                  .Cacheable(serviceProvider)
                  .SingleOrDefaultAsync();
                Console.WriteLine($"2,en -> item.Id= {item.Id}"); // --> 3

                Console.WriteLine($"{Environment.NewLine}3,en");
                /*
                SELECT TOP(2) [u].[Id], [u].[IdPaymentType], [u].[Language]
                FROM [Payments] AS [u]
                WHERE ([u].[IdPaymentType] = 3) AND ([u].[Language] = N'en')
                */
                item = await context.Payments
                  .Where(u => u.IdPaymentType == 3 && u.Language.Equals("en"))
                  .Cacheable(serviceProvider)
                  .SingleOrDefaultAsync();
                Console.WriteLine($"3,en -> item.Id= {item.Id}");  // --> 5

                Console.WriteLine($"{Environment.NewLine}3,pl");
                /*
                SELECT [u].[Id], [u].[IdPaymentType], [u].[Language]
                FROM [Payments] AS [u]
                WHERE ([u].[IdPaymentType] = 3) AND ([u].[Language] = N'pl')
                */
                item = await context.Payments
                                  .Where(u => u.IdPaymentType == 3 && u.Language.Equals("pl"))
                                  .Cacheable(serviceProvider)
                                  .SingleOrDefaultAsync();
                Console.WriteLine($"3,pl -> item.Id= {item.Id}");  // --> 6

                Console.WriteLine($"{Environment.NewLine}2,pl");
                /*
                SELECT TOP(2) [u].[Id], [u].[IdPaymentType], [u].[Language]
                FROM [Payments] AS [u]
                WHERE ([u].[IdPaymentType] = 2) AND ([u].[Language] = N'pl')
                */
                item = await context.Payments
                  .Where(u => u.IdPaymentType == 2 && u.Language.Equals("pl"))
                  .Cacheable(serviceProvider)
                  .SingleOrDefaultAsync();
                Console.WriteLine($"2,pl -> item.Id= {item.Id}");  // --> 4

                Console.WriteLine($"{Environment.NewLine}1,pl");
                /*
                SELECT [u].[Id], [u].[IdPaymentType], [u].[Language]
                FROM [Payments] AS [u]
                WHERE ([u].[IdPaymentType] = 1) AND ([u].[Language] = N'pl')
                */
                item = await context.Payments
                  .Where(u => u.IdPaymentType == 1 && u.Language.Equals("pl"))
                  .Cacheable(serviceProvider)
                  .SingleOrDefaultAsync();
                Console.WriteLine($"1,pl -> item.Id= {item.Id}");  // --> 2
            }

            Console.WriteLine("Press a key ...");
            Console.ReadKey();
        }

        private static void SetupDatabase()
        {
            using (var db = new SampleContext())
            {
                if (db.Database.EnsureCreated())
                {
                    var item1 = new Payment { IdPaymentType = 1, Language = "en" };
                    db.Payments.Add(item1);

                    var item2 = new Payment { IdPaymentType = 1, Language = "pl" };
                    db.Payments.Add(item2);

                    var item3 = new Payment { IdPaymentType = 2, Language = "en" };
                    db.Payments.Add(item3);

                    var item4 = new Payment { IdPaymentType = 2, Language = "pl" };
                    db.Payments.Add(item4);

                    var item5 = new Payment { IdPaymentType = 3, Language = "en" };
                    db.Payments.Add(item5);

                    var item6 = new Payment { IdPaymentType = 3, Language = "pl" };
                    db.Payments.Add(item6);

                    db.SaveChanges();
                }
            }
        }
    }
}
