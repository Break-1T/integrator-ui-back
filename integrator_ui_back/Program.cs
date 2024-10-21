using integrator_ui_back.Extensions;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Listen(IPAddress.Any, 8080);
});

builder.Host.UseSerilog((whb, loggerConfiguration) =>
{
    loggerConfiguration
        .WriteTo.Console()
        .WriteTo.Debug();
}, writeToProviders: true, preserveStaticLogger: false);

builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddIntegrationUI(builder.Configuration, builder.Environment);

builder.Services.AddCors();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddMvcCore()
    .AddDataAnnotations()
    .AddApiExplorer()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(context.ModelState);
    });

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true)
    .AllowCredentials());

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
