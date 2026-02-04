var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();

var app = builder.Build();

app.MapGet("/customers/{id}", (Guid id) =>
{
    // Hardcoded customer used for development and tests until a real data store is available
    var customer = new
    {
        Name = "Lucas Lo Faro",
        Mail = "lucaslofaro@gmail.com",
        Phone = "1160456045",
        Address = new
        {
            Street = "Soldado de la independencia",
            Number = "1111",
            ZipCode = "1426",
            City = "Buenos Aires",
            AdditionalInformation = "Piso 32 Departamento A"
        }
    };

    return Results.Ok(customer);
});

app.UseDefaultEndpoints();

app.Run();
