namespace Itm.Booking.Api.Dtos;

/// <summary>
/// DTO para recibir respuesta de Event.Api.
/// </summary>
public record EventDto(int EventId, string Name, decimal PriceBase, int AvailableSeats);
