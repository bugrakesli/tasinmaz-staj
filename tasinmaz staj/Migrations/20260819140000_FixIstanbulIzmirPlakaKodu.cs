using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace tasinmaz_staj.Migrations
{
    /// <inheritdoc />
    public partial class FixIstanbulIzmirPlakaKodu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Veritabanında "İstanbul" ve "İzmir" noktalı büyük İ ile değil,
            // düz Latin "I" ile kayıtlı (Istanbul, Izmir). Önceki migration'daki
            // isim eşleştirmesi bu yüzden bu iki ili atlamıştı. Burada hem
            // PlakaKodu'nu dolduruyor hem de Ad değerini doğru Türkçe
            // yazıma (İstanbul / İzmir) düzeltiyoruz.
            migrationBuilder.Sql(
                "UPDATE \"Iller\" SET \"Ad\" = 'İstanbul', \"PlakaKodu\" = 34 WHERE TRIM(\"Ad\") = 'Istanbul';");

            migrationBuilder.Sql(
                "UPDATE \"Iller\" SET \"Ad\" = 'İzmir', \"PlakaKodu\" = 35 WHERE TRIM(\"Ad\") = 'Izmir';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Iller\" SET \"Ad\" = 'Istanbul', \"PlakaKodu\" = NULL WHERE TRIM(\"Ad\") = 'İstanbul';");

            migrationBuilder.Sql(
                "UPDATE \"Iller\" SET \"Ad\" = 'Izmir', \"PlakaKodu\" = NULL WHERE TRIM(\"Ad\") = 'İzmir';");
        }
    }
}
