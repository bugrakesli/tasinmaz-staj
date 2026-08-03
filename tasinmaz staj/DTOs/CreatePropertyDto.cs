public class CreatePropertyDto
{
    public int MahalleId { get; set; }
    public string ParselNo { get; set; }
    public string AdaNo { get; set; }
    public string Adres { get; set; }
    public string PropertyType { get; set; }
    public string Coordinate { get; set; }  // 4 noktalý WKT polygon
}