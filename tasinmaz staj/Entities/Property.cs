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
    public string Coordinate { get; set; }   // WKT formatýnda polygon
    public string ImagePath { get; set; }
}