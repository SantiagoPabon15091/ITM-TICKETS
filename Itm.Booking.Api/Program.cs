using System.Net.Http.Json;
using Itm.Booking.Api.Dtos;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpClientFactory con resiliencia
builder.Services
    .AddHttpClient("EventClient", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5201");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient("DiscountClient", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5202");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardResilienceHandler();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// POST /api/bookings — Patrón SAGA
app.MapPost("/api/bookings", async (BookingRequest request, IHttpClientFactory factory) =>
{
    var eventClient = factory.CreateClient("EventClient");
    var discountClient = factory.CreateClient("DiscountClient");

    var eventTask = eventClient.GetAsync($"/api/events/{request.EventId}");
    var discountTask = discountClient.GetAsync($"/api/discounts/{request.DiscountCode}");

    await Task.WhenAll(eventTask, discountTask);

    var eventResponse = await eventTask;
    var discountResponse = await discountTask;

    if (!eventResponse.IsSuccessStatusCode)
        return Results.BadRequest("El evento no existe o no está disponible.");

    if (!discountResponse.IsSuccessStatusCode)
        return Results.NotFound("No se encontró el código.");

    var eventData = await eventResponse.Content.ReadFromJsonAsync<EventDto>();
    var discountData = await discountResponse.Content.ReadFromJsonAsync<DiscountDto>();

    if (eventData is null)
        return Results.Problem("Error al obtener datos del evento");

    if (discountData is null)
        return Results.Problem("El codigo de descuento no es valido");

    var subtotal = eventData.PriceBase * request.Tickets;
    var discountAmount = subtotal * discountData.Percentage;
    var total = subtotal - discountAmount;
    app.Logger.LogInformation("[Booking] Evento {EventId}, {Tickets} entradas, subtotal {Subtotal}, descuento {Discount}, total {Total}", request.EventId, request.Tickets, subtotal, discountAmount, total);

    var reserveResponse = await eventClient.PostAsJsonAsync("/api/events/reserve",
        new { EventId = request.EventId, Quantity = request.Tickets });

    if (!reserveResponse.IsSuccessStatusCode)
        return Results.BadRequest("No hay sillas suficientes o el evento no existe.");

    try
    {
        bool paymentSuccess = new Random().Next(1, 10) > 5;

        if (!paymentSuccess)
            throw new Exception("Fondos insuficientes en la tarjeta.");

        app.Logger.LogInformation("[Booking] Pago exitoso. Reserva completada.");
        return Results.Ok(new
        {
            Status = "Success",
            Message = "¡Disfruta el concierto ITM!",
            Total = total
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("[SAGA] Error en pago: {Message}. Liberando sillas...", ex.Message);
        await eventClient.PostAsJsonAsync("/api/events/release",
            new { EventId = request.EventId, Quantity = request.Tickets });
        return Results.Problem("Tu pago fue rechazado. Tus sillas fueron liberadas.");
    }
})
.WithName("CreateBooking")
.WithOpenApi();

app.Run();
