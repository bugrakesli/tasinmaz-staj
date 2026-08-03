using System.ComponentModel.DataAnnotations;

public class CreatePropertyDto
{
    [Required(ErrorMessage = "Þehir alaný zorunludur.")]
    public string City { get; set; }

    [Required(ErrorMessage = "Ýlçe alaný zorunludur.")]
    public string District { get; set; }

    [Required(ErrorMessage = "Mahalle alaný zorunludur.")]
    public string Neighborhood { get; set; }

    [Required(ErrorMessage = "Ada numarasý zorunludur.")]
    public string LotNumber { get; set; }

    [Required(ErrorMessage = "Parsel numarasý zorunludur.")]
    public string ParcelNumber { get; set; }

    [Required(ErrorMessage = "Adres zorunludur.")]
    public string Address { get; set; }

    [Required(ErrorMessage = "Taþýnmaz tipi zorunludur.")]
    public string PropertyType { get; set; }

    [Required(ErrorMessage = "Koordinat (WKT) bilgisi zorunludur.")]
    public string Coordinate { get; set; } 
}