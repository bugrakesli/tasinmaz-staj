public class IlDto
{
    public int Id { get; set; }
    public string Ad { get; set; }
}

public class IlceDto
{
    public int Id { get; set; }
    public int IlId { get; set; }
    public string Ad { get; set; }
}

public class MahalleDto
{
    public int Id { get; set; }
    public int IlceId { get; set; }
    public string Ad { get; set; }
}
