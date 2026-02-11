using MassTransit;
using NotificationService;
using NotificationService.Templates;
using ServiceDefaults;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IEmailSender, SendGridEmailSender>();
builder.Services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();

builder.AddServiceDefaults()
       .WithMassTransit(assemblies: Assembly.GetExecutingAssembly());

var host = builder.Build();
host.Run();
