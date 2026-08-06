public class GeometryOperationResultDto
{
    public string Label { get; set; }              // "D", "E" veya kesişim için null
    public string Wkt { get; set; }
    public double SurfaceAreaSquareMeters { get; set; }
    public bool Saved { get; set; }                 // union=true, intersection=false (SRS: kesişim kaydedilmez)
    public bool HasIntersection { get; set; } = true; // intersection yoksa false + mesaj
    public string Message { get; set; }
}