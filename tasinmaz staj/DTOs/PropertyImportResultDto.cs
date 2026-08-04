using System.Collections.Generic;

public class PropertyImportResultDto
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}
