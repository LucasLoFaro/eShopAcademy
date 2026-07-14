using MassTransit;
using NotificationService;
using NotificationService.Data;
using NotificationService.Templates;
using ServiceDefaults;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
builder.Services.AddSingleton(
    new NotificationDbContext(builder.Configuration.GetConnectionString("notifications")!, "notifications"));

builder.AddServiceDefaults()
       .WithMassTransit(assemblies: Assembly.GetExecutingAssembly());

var host = builder.Build();
host.Run();
