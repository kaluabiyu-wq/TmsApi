using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

// Naming standard: Create{Entity}Request / Update{Entity}Request / {Entity}Response.
// Entities are never returned from the API directly.

public record CreateClassroomRequest
{
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    [MaxLength(200)]
    public string Location { get; init; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000.")]
    public int Capacity { get; init; }
}

public record UpdateClassroomRequest
{
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    [MaxLength(200)]
    public string Location { get; init; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Capacity must be between 1 and 1000.")]
    public int Capacity { get; init; }

    public bool IsActive { get; init; } = true;
}

public record ClassroomResponse(
    int Id,
    string Name,
    string Location,
    int Capacity,
    bool IsActive,
    DateTime CreatedAt);
