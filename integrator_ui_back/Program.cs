using integrator_ui_back.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});
app.MapReverseProxy();

app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin()
    .AllowCredentials());

app.Run();
