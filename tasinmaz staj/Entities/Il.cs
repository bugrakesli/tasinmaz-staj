using System.Collections.Generic;

public class Il
{
    public int Id { get; set; }
    public string Ad { get; set; }

    // Türkiye plaka kodu (1=Adana ... 81=Düzce). Id sütunu seed sırasına
    // göre atandığı için plaka numarasıyla örtüşmüyor; dropdown'ı plaka
    // sırasına göre listelemek için ayrı bir kolon gerekiyor.
    public int? PlakaKodu { get; set; }

    public ICollection<Ilce> Ilceler { get; set; }
}