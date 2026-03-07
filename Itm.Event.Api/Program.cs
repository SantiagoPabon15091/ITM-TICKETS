using Itm.Event.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Base de datos
var eventsDb = new List<EventEntity>
{
    new(1, "Concierto ITM", 50000, 100)
};

// get /api/events/{id} — Retorna evento con EventId, Name, PriceBase, AvailableSeats
app.MapGet("/api/events/{id}", (int id) =>
{
    var entity = eventsDb.FirstOrDefault(e => e.Id == id);
    if (entity is null)
        return Results.NotFound();

    var dto = new EventDto(entity.Id, entity.Name, entity.PriceBase, entity.AvailableSeats);
    return Results.Ok(dto);
})
.WithName("GetEvent")
.WithOpenApi();

// POST /api/events/reserve — Reservar sillas
app.MapPost("/api/events/reserve", (ReserveSeatsRequestDto request) =>
{
    var entity = eventsDb.FirstOrDefault(e => e.Id == request.EventId);
    if (entity is null)
        return Results.BadRequest("Evento no existe.");

    if (entity.AvailableSeats < request.Quantity)
        return Results.BadRequest("No hay sillas suficientes.");

    var index = eventsDb.IndexOf(entity);
    eventsDb[index] = entity with { AvailableSeats = entity.AvailableSeats - request.Quantity };
    app.Logger.LogInformation("[Event.Api] Reservadas {Quantity} sillas para evento {EventId}. Restan {Rest}", request.Quantity, request.EventId, eventsDb[index].AvailableSeats);
    return Results.Ok(new { Message = "Sillas reservadas.", AvailableSeats = eventsDb[index].AvailableSeats });
})
.WithName("ReserveSeats")
.WithOpenApi();

// POST /api/events/release — Liberar sillas
app.MapPost("/api/events/release", (ReleaseSeatsRequestDto request) =>
{
    var entity = eventsDb.FirstOrDefault(e => e.Id == request.EventId);
    if (entity is null)
        return Results.BadRequest("Evento no existe.");

    var index = eventsDb.IndexOf(entity);
    eventsDb[index] = entity with { AvailableSeats = entity.AvailableSeats + request.Quantity };
    app.Logger.LogInformation("[Event.Api] Liberadas {Quantity} sillas para evento {EventId}. Disponibles: {Available}", request.Quantity, request.EventId, eventsDb[index].AvailableSeats);
    return Results.Ok(new { Message = "Sillas liberadas.", AvailableSeats = eventsDb[index].AvailableSeats });
})
.WithName("ReleaseSeats")
.WithOpenApi();

app.Run();

// Modelo interno (no expuesto en la API)
file record EventEntity(int Id, string Name, decimal PriceBase, int AvailableSeats);
