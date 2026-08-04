public class IntersectionResultDto
{
    public int PropertyId { get; set; }

    public bool Intersects { get; set; }

    public double PropertyAreaSquareMeters { get; set; }

    public double IntersectionAreaSquareMeters { get; set; }

    public double IntersectionPercentage { get; set; }

    public string IntersectionGeometry { get; set; }
}