using System.ComponentModel.DataAnnotations;

namespace PotyInternosAPI.DTOs.Areas;

public class AreaResponseDto
{
    public string AreaId { get; set; } = null!;
    public string Area { get; set; } = null!;
    public bool Status { get; set; }
}

public class AreaCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Area { get; set; } = null!;
}

public class AreaUpdateDto
{
    [Required]
    [MaxLength(200)]
    public string Area { get; set; } = null!;

    public bool Status { get; set; }
}
