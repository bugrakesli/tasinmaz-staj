using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace tasinmaz_staj.Data
{
    public static class DataSeeder
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using var context = new RemsDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<RemsDbContext>>());

            if (context.Iller.Any())
            {
                return; // Veritabanında il verisi varsa seed işlemi yapma
            }

            var iller = new List<Il>
            {
                new Il
                {
                    Ad = "İstanbul",
                    Ilceler = new List<Ilce>
                    {
                        new Ilce { Ad = "Kadıköy", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Caferağa" }, new Mahalle { Ad = "Osmanağa" } } },
                        new Ilce { Ad = "Beşiktaş", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Bebek" }, new Mahalle { Ad = "Levent" } } },
                        new Ilce { Ad = "Üsküdar", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Kuzguncuk" }, new Mahalle { Ad = "Çengelköy" } } }
                    }
                },
                new Il
                {
                    Ad = "Ankara",
                    Ilceler = new List<Ilce>
                    {
                        new Ilce { Ad = "Çankaya", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Kızılay" }, new Mahalle { Ad = "Bahçelievler" } } },
                        new Ilce { Ad = "Keçiören", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Etlik" }, new Mahalle { Ad = "İncirli" } } },
                        new Ilce { Ad = "Yenimahalle", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Batıkent" }, new Mahalle { Ad = "Demetevler" } } }
                    }
                },
                new Il
                {
                    Ad = "İzmir",
                    Ilceler = new List<Ilce>
                    {
                        new Ilce { Ad = "Karşıyaka", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Bostanlı" }, new Mahalle { Ad = "Mavişehir" } } },
                        new Ilce { Ad = "Bornova", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Erzene" }, new Mahalle { Ad = "Evka 3" } } },
                        new Ilce { Ad = "Konak", Mahalleler = new List<Mahalle> { new Mahalle { Ad = "Alsancak" }, new Mahalle { Ad = "Göztepe" } } }
                    }
                }
            };

            context.Iller.AddRange(iller);
            context.SaveChanges();
        }
    }
}
