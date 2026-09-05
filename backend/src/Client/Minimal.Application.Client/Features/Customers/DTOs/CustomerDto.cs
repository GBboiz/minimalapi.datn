namespace MinimalAPI.Application.Features.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt);
