namespace Itm.Event.Api.Dtos;

/// <summary>
/// DTO de entrada para liberar sillas (compensación SAGA).
/// </summary>
public record ReleaseSeatsRequestDto(int EventId, int Quantity);
