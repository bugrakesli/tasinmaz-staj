using System.ComponentModel.DataAnnotations;

public class SaveGeometryDto
{
    [Required]
    public string ResultType { get; set; } // SRS'e göre "D" veya "E" gelecek[cite: 1]

    [Required]
    public string ResultWkt { get; set; } // Frontend'in birleþtirdiði yeni poligon koordinatlarý

    [Required]
    public double CalculatedArea { get; set; } // Frontend'den gelecek metrekare ($m^2$) cinsinden alan[cite: 1]
}