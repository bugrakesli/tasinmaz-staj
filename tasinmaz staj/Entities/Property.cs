using NetTopologySuite.Geometries;

public class Property
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public int MahalleId { get; set; }
    public Mahalle Mahalle { get; set; }

    public string ParselNo { get; set; }
    public string AdaNo { get; set; }
    public string Adres { get; set; }
    public string PropertyType { get; set; }

    
    public Polygon Geometry { get; set; } // NetTopologySuite.Geometries.Polygon

    public string ImagePath { get; set; }
}