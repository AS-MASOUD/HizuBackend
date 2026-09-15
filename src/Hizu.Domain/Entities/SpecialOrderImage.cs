using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class SpecialOrderImage : BaseEntity
{
    public Guid SpecialOrderId { get; set; }
    public SpecialOrder SpecialOrder { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long FileSizeBytes { get; set; }
}
