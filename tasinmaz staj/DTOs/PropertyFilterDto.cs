public class PropertyFilterDto
{
    public string City { get; set; }
    public string District { get; set; }
    public string Neighborhood { get; set; }
    public string ParcelNumber { get; set; }
    public string LotNumber { get; set; }
    public string Address { get; set; }
    public string PropertyType { get; set; }
    public int? OwnerId { get; set; }

    // Sayfalama
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}