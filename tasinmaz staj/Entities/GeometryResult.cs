using System;

public class GeometryResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Label { get; set; }
    public string Wkt { get; set; }
    public double SurfaceArea { get; set; }
    public DateTime CreatedAt { get; set; }
}