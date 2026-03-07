namespace Itm.Event.Api.Dtos;

/// <summary>
/// DTO de respuesta para consulta de evento.
/// </summary>
public record EventDto(int EventId, string Name, decimal PriceBase, int AvailableSeats);
