using Itm.Discount.Api.Dtos;

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

// DB
var discountsDb = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
{
    ["ITM50"] = 0.5m 
};

app.MapGet("/api/discounts/{code}", (string code) =>
{
    if (!discountsDb.TryGetValue(code, out var percentage))
        return Results.NotFound();

    var dto = new DiscountDto(percentage);
    return Results.Ok(dto);
})
.WithName("GetDiscount")
.WithOpenApi();

app.Run();
