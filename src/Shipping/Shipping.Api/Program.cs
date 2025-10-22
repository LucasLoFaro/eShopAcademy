var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
       .WithSwagger();

// TODO: Add a minimal webhook notification processing endpoint to handle shipping status updates

var app = builder.Build();

app.Run();
