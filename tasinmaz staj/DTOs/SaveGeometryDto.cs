using System.ComponentModel.DataAnnotations;

public class SaveManualGeometryDto
{
    [Required]
    [RegularExpression("^(A|B|C)$", ErrorMessage = "Label 'A', 'B' veya 'C' olmalıdır.")]
    public string Label { get; set; }

    [Required]
    public string Wkt { get; set; } // Kullanıcının haritada çizdiği polygon (WKT)
}