using System;

public class GeometryResult
{
    public int Id { get; set; }
    public string Label { get; set; }      // "D" veya "E"
    public string Wkt { get; set; }        // hesaplanan geometri
    public double SurfaceArea { get; set; } // m²
    public DateTime CreatedAt { get; set; }
}