using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using TasinmazStaj.Interfaces;

namespace TasinmazStaj.Services
{
    public class GeometryService : IGeometryService
    {
        private readonly RemsDbContext _context;
        private readonly WKTReader _wktReader = new WKTReader();

        private static readonly string[] ManualLabels = { "A", "B", "C" };

        public GeometryService(RemsDbContext context)
        {
            _context = context;
        }

        // ---------- Manual Draw: A, B veya C kaydet/güncelle ----------
        public async Task<GeometryResult> SaveManualGeometryAsync(SaveManualGeometryDto dto, int userId)
        {
            Geometry geometry;
            try
            {
                geometry = _wktReader.Read(dto.Wkt);
            }
            catch
            {
                throw new ArgumentException("Geçersiz WKT formatı.");
            }

            if (geometry == null || geometry.GeometryType != "Polygon" || !geometry.IsValid)
            {
                throw new ArgumentException("Geçerli bir polygon (WKT) girilmelidir.");
            }

            double areaSqMeters = CalculateAreaInSquareMeters(geometry);

            // Aynı kullanıcı için aynı etiket (A/B/C) daha önce kaydedilmişse üzerine yaz
            var existing = await _context.GeometryResults
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Label == dto.Label);

            if (existing != null)
            {
                existing.Wkt = geometry.AsText();
                existing.SurfaceArea = areaSqMeters;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new GeometryResult
                {
                    UserId = userId,
                    Label = dto.Label,
                    Wkt = geometry.AsText(),
                    SurfaceArea = areaSqMeters,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GeometryResults.Add(existing);
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        // ---------- Auto-Select: kullanıcının kayıtlı A, B, C'sini getir ----------
        public async Task<List<GeometryResult>> GetAutoSelectGeometriesAsync(int userId)
        {
            var results = await _context.GeometryResults
                .Where(g => g.UserId == userId && ManualLabels.Contains(g.Label))
                .ToListAsync();

            if (results.Count < 3)
            {
                // REQ-7: "No saved geometries found. Please use Manual Draw."
                throw new KeyNotFoundException(
                    "No saved geometries found. Please use Manual Draw.");
            }

            return results;
        }

        // ---------- A ∩ B (veya B ∩ A — matematiksel olarak aynı) ----------
        public async Task<GeometryOperationResultDto> ComputeIntersectionAsync(int userId)
        {
            var (a, b, _) = await GetRequiredGeometriesAsync(userId, includeC: false);

            var geomA = _wktReader.Read(a.Wkt);
            var geomB = _wktReader.Read(b.Wkt);

            bool intersects = geomA.Intersects(geomB);

            if (!intersects)
            {
                return new GeometryOperationResultDto
                {
                    HasIntersection = false,
                    Saved = false,
                    Message = "No intersection found."
                };
            }

            var intersection = geomA.Intersection(geomB);
            double area = CalculateAreaInSquareMeters(intersection);

            return new GeometryOperationResultDto
            {
                HasIntersection = true,
                Saved = false, // REQ-13: sadece union kaydedilir, kesişim sadece gösterilir
                Wkt = intersection.AsText(),
                SurfaceAreaSquareMeters = Math.Round(area, 2)
            };
        }

        // ---------- A ∪ B (→ D) veya A ∪ B ∪ C (→ E) ----------
        public async Task<GeometryOperationResultDto> ComputeUnionAsync(int userId, bool includeC)
        {
            var (a, b, c) = await GetRequiredGeometriesAsync(userId, includeC);

            var geomA = _wktReader.Read(a.Wkt);
            var geomB = _wktReader.Read(b.Wkt);

            Geometry unionGeometry = geomA.Union(geomB);
            string label = "D";

            if (includeC)
            {
                var geomC = _wktReader.Read(c.Wkt);
                unionGeometry = unionGeometry.Union(geomC);
                label = "E";
            }

            double area = CalculateAreaInSquareMeters(unionGeometry);

            // D veya E için önceki sonucu üzerine yaz
            var existing = await _context.GeometryResults
                .FirstOrDefaultAsync(g => g.UserId == userId && g.Label == label);

            if (existing != null)
            {
                existing.Wkt = unionGeometry.AsText();
                existing.SurfaceArea = area;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new GeometryResult
                {
                    UserId = userId,
                    Label = label,
                    Wkt = unionGeometry.AsText(),
                    SurfaceArea = area,
                    CreatedAt = DateTime.UtcNow
                };
                _context.GeometryResults.Add(existing);
            }

            await _context.SaveChangesAsync();

            return new GeometryOperationResultDto
            {
                Label = label,
                Wkt = unionGeometry.AsText(),
                SurfaceAreaSquareMeters = Math.Round(area, 2),
                Saved = true,
                HasIntersection = true
            };
        }

        // ---------- Clear: kullanıcının A/B/C/D/E kayıtlarını sil ----------
        public async Task ClearAsync(int userId)
        {
            var all = _context.GeometryResults.Where(g => g.UserId == userId);
            _context.GeometryResults.RemoveRange(all);
            await _context.SaveChangesAsync();
        }

        // ---------- Yardımcılar ----------
        private async Task<(GeometryResult a, GeometryResult b, GeometryResult c)> GetRequiredGeometriesAsync(
            int userId, bool includeC)
        {
            var labels = includeC ? new[] { "A", "B", "C" } : new[] { "A", "B" };

            var results = await _context.GeometryResults
                .Where(g => g.UserId == userId && labels.Contains(g.Label))
                .ToListAsync();

            if (results.Count < labels.Length)
            {
                // REQ-5: "Please complete geometries A, B, and C."
                throw new ArgumentException("Please complete geometries A, B, and C.");
            }

            var a = results.First(g => g.Label == "A");
            var b = results.First(g => g.Label == "B");
            var c = includeC ? results.First(g => g.Label == "C") : null;

            return (a, b, c);
        }

        // TODO(bilinen sinirlama): Web Mercator (EPSG:3857) alan koruyucu degil,
        // Turkiye enlemlerinde (~36-42 derece) alani ~1.6-1.8x sismis gosteriyor.
        // Ayni sorun PropertyGeometryService.AnalyzeIntersectionAsync/AnalyzeUnionAsync'te
        // de var (EF.Functions.Transform(...,3857)). Test asamasinda geodesic
        // formule (Chamberlain & Duquette) gecilecek. Simdilik bilerek erteleniyor.
        private static double CalculateAreaInSquareMeters(Geometry geometry)
        {
            var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 3857);

            Coordinate[] Project(Coordinate[] coords) => coords
                .Select(c => new Coordinate(
                    c.X * 20037508.34 / 180.0,
                    Math.Log(Math.Tan((90 + c.Y) * Math.PI / 360.0)) / (Math.PI / 180.0) * 20037508.34 / 180.0
                ))
                .ToArray();

            if (geometry is Polygon polygon)
            {
                var shell = factory.CreateLinearRing(Project(polygon.ExteriorRing.Coordinates));
                var holes = polygon.InteriorRings
                    .Select(r => factory.CreateLinearRing(Project(r.Coordinates)))
                    .ToArray();
                return factory.CreatePolygon(shell, holes).Area;
            }

            // Union/Intersection sonucu MultiPolygon dönebilir
            if (geometry is MultiPolygon multi)
            {
                double total = 0;
                foreach (Polygon p in multi.Geometries)
                {
                    total += CalculateAreaInSquareMeters(p);
                }
                return total;
            }

            return 0;
        }
    }
}