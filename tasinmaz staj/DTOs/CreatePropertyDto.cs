using System.ComponentModel.DataAnnotations;

public class CreatePropertyDto
{
    [Required(ErrorMessage = "Şehir alanı zorunludur.")]
    public string City { get; set; }

    [Required(ErrorMessage = "İlçe alanı zorunludur.")]
    public string District { get; set; }

    [Required(ErrorMessage = "Mahalle alanı zorunludur.")]
    public string Neighborhood { get; set; }

    [Required(ErrorMessage = "Ada numarası zorunludur.")]
    public string LotNumber { get; set; }

    [Required(ErrorMessage = "Parsel numarası zorunludur.")]
    public string ParcelNumber { get; set; }

    [Required(ErrorMessage = "Adres zorunludur.")]
    public string Address { get; set; }

    [Required(ErrorMessage = "Taşınmaz tipi zorunludur.")]
    public string PropertyType { get; set; }

    [Required(ErrorMessage = "Koordinat (WKT) bilgisi zorunludur.")]
    public string Coordinate { get; set; } 
}