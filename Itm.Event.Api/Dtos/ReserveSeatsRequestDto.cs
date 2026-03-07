namespace Itm.Event.Api.Dtos;

/// <summary>
/// DTO de entrada para reservar sillas.
/// </summary>
public record ReserveSeatsRequestDto(int EventId, int Quantity);
