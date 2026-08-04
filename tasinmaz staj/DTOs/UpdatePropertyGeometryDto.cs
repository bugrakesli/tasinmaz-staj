using System.Collections.Generic;

public class UpdatePropertyGeometryDto
{
	public List<List<CoordinateDto>> Coordinates { get; set; }
}

public class CoordinateDto
{
	public double Longitude { get; set; }
	public double Latitude { get; set; }
}
