using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class PropertyImportService : IPropertyImportService
{
    private readonly RemsDbContext _context;

    // REQ-2: Zorunlu sutunlar. Basliklar, Export Ozelligiyle ayni sirada ve
    // isimde tutuldu; kullanici sablon olarak once export edip sonra
    // doldurarek geri yukleyebilir.
    private static readonly string[] RequiredHeaders =
    {
        "Şehir", "İlçe", "Mahalle", "Ada No", "Parsel No",
        "Adres", "Taşınmaz Tipi", "Koordinat (WKT)"
    };

    private readonly WKTReader _wktReader = new WKTReader();

    public PropertyImportService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<PropertyImportResultDto> ImportFromExcelAsync(Stream fileStream, int userId)
    {
        var result = new PropertyImportResultDto();

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch
        {
            // REQ-5: bozuk/gecersiz .xlsx dosyasi
            result.Success = false;
            result.Errors.Add("Import failed. Please check the file format and data.");
            return result;
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                result.Success = false;
                result.Errors.Add("Import failed. Please check the file format and data.");
                return result;
            }

            var headerRow = sheet.Row(1);
            var columnMap = new Dictionary<string, int>();

            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (int col = 1; col <= lastColumn; col++)
            {
                var header = headerRow.Cell(col).GetString().Trim();
                if (!string.IsNullOrEmpty(header))
                    columnMap[header] = col;
            }

            // REQ-2: gerekli sutunlarin varligini dogrula
            var missingHeaders = RequiredHeaders
                .Where(h => !columnMap.ContainsKey(h))
                .ToList();

            if (missingHeaders.Any())
            {
                result.Success = false;
                result.Errors.Add(
                    $"Import failed. Please check the file format and data. " +
                    $"Eksik sütun(lar): {string.Join(", ", missingHeaders)}");
                return result;
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

            var rowsToImport = new List<Property>();
            var errors = new List<string>();

            // Bu bellek-ici cache ile ayni sehir/ilce/mahalle icin
            // tekrar tekrar veritabani sorgusu atmiyoruz.
            var mahalleCache = new Dictionary<(string city, string district, string neighborhood), Mahalle>();

            for (int row = 2; row <= lastRow; row++)
            {
                var rowCells = sheet.Row(row);

                // Tamamen bos satirlari atla (Excel'de trailing bos satirlar olabilir)
                bool isRowEmpty = RequiredHeaders.All(h =>
                    string.IsNullOrWhiteSpace(rowCells.Cell(columnMap[h]).GetString()));
                if (isRowEmpty)
                    continue;

                string GetCell(string header) => rowCells.Cell(columnMap[header]).GetString().Trim();

                var city = GetCell("Şehir");
                var district = GetCell("İlçe");
                var neighborhood = GetCell("Mahalle");
                var lotNumber = GetCell("Ada No");
                var parcelNumber = GetCell("Parsel No");
                var address = GetCell("Adres");
                var propertyType = GetCell("Taşınmaz Tipi");
                var coordinate = GetCell("Koordinat (WKT)");

                // REQ-3: tum zorunlu alanlar dolu olmali
                var emptyFields = new List<string>();
                if (string.IsNullOrWhiteSpace(city)) emptyFields.Add("Şehir");
                if (string.IsNullOrWhiteSpace(district)) emptyFields.Add("İlçe");
                if (string.IsNullOrWhiteSpace(neighborhood)) emptyFields.Add("Mahalle");
                if (string.IsNullOrWhiteSpace(lotNumber)) emptyFields.Add("Ada No");
                if (string.IsNullOrWhiteSpace(parcelNumber)) emptyFields.Add("Parsel No");
                if (string.IsNullOrWhiteSpace(address)) emptyFields.Add("Adres");
                if (string.IsNullOrWhiteSpace(propertyType)) emptyFields.Add("Taşınmaz Tipi");
                if (string.IsNullOrWhiteSpace(coordinate)) emptyFields.Add("Koordinat (WKT)");

                if (emptyFields.Any())
                {
                    errors.Add($"Satır {row}: Boş alan(lar): {string.Join(", ", emptyFields)}");
                    continue;
                }

                // REQ-3: koordinat gecerli bir WKT polygon mu?
                NetTopologySuite.Geometries.Polygon parsedGeometry;
                try
                {
                    var geometry = _wktReader.Read(coordinate);
                    if (geometry == null || geometry.GeometryType != "Polygon")
                    {
                        errors.Add($"Satır {row}: Koordinat geçerli bir polygon (WKT) değil.");
                        continue;
                    }

                    parsedGeometry = (NetTopologySuite.Geometries.Polygon)geometry;
                    if (parsedGeometry.SRID == 0)
                    {
                        parsedGeometry.SRID = 4326;
                    }
                }
                catch
                {
                    errors.Add($"Satır {row}: Koordinat geçerli bir WKT formatında değil.");
                    continue;
                }

                // Sehir/ilce/mahalle eslesmesi
                var cacheKey = (city, district, neighborhood);
                if (!mahalleCache.TryGetValue(cacheKey, out var mahalle))
                {
                    mahalle = await _context.Mahalleler
                        .FirstOrDefaultAsync(m =>
                            m.Ad == neighborhood &&
                            m.Ilce.Ad == district &&
                            m.Ilce.Il.Ad == city);
                    mahalleCache[cacheKey] = mahalle;
                }

                if (mahalle == null)
                {
                    errors.Add(
                        $"Satır {row}: '{city} / {district} / {neighborhood}' " +
                        "eşleşen bir şehir/ilçe/mahalle kaydı bulunamadı.");
                    continue;
                }

                rowsToImport.Add(new Property
                {
                    UserId = userId, // REQ-6: mevcut kullaniciya otomatik baglan
                    MahalleId = mahalle.Id,
                    AdaNo = lotNumber,
                    ParselNo = parcelNumber,
                    Adres = address,
                    PropertyType = propertyType,
                    Coordinate = coordinate,
                    Geometry = parsedGeometry,
                    ImagePath = null
                });
            }

            // REQ-4: herhangi bir satirda hata varsa dosyanin tamami reddedilir,
            // hicbir kayit veritabanina yazilmaz.
            if (errors.Any())
            {
                result.Success = false;
                result.Errors.Add("Import failed. Please check the file format and data.");
                result.Errors.AddRange(errors);
                return result;
            }

            if (!rowsToImport.Any())
            {
                result.Success = false;
                result.Errors.Add("Import failed. Please check the file format and data. Dosyada içe aktarılacak satır bulunamadı.");
                return result;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            _context.Properties.AddRange(rowsToImport);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.Success = true;
            result.ImportedCount = rowsToImport.Count;
            return result;
        }
    }
}
