namespace Itm.Booking.Api.Dtos;

/// <summary>
/// DTO de entrada para crear una reserva
/// </summary>
public record BookingRequest(int EventId, int Tickets, string DiscountCode);
