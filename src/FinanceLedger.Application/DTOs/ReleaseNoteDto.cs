using FinanceLedger.Domain.Enums;

namespace FinanceLedger.Application.DTOs;

public class ReleaseNoteDto
{
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ReleaseType Type { get; set; }
    public string[] Notes { get; set; } = Array.Empty<string>();
    public string PublishedBy { get; set; } = string.Empty;
    public string PublishedByName { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; }
}

public class CreateReleaseNoteDto
{
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ReleaseType Type { get; set; }
    public string[] Notes { get; set; } = Array.Empty<string>();
}

public class UpdateReleaseNoteDto
{
    public string Version { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public ReleaseType Type { get; set; }
    public string[] Notes { get; set; } = Array.Empty<string>();
}
