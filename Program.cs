using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// 1. AGREGAR SERVICIOS AL CONTENEDOR
builder.Services.AddControllers();

// 2. REGISTRAR SERVICIOS PROPIOS
// SignerService no necesita dependencias
builder.Services.AddScoped<SummaCore.Services.SignerService>();

// DgiiService necesita SignerService y ILogger
builder.Services.AddScoped<SummaCore.Services.DgiiService>();

// 3. CONFIGURAR LOGGING (para Azure)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // Para desarrollo local
builder.Logging.AddDebug();   // Para debugging
builder.Logging.AddAzureWebAppDiagnostics(); // Para Azure App Service

var app = builder.Build();

// 4. CONFIGURAR EL PIPELINE HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// 5. ACTIVAR LAS RUTAS
app.MapControllers();

// Mensaje por defecto en la raíz
app.MapGet("/", () => "¡SummaCore API está corriendo! Usa /api/pruebas/test-autenticacion");

app.Run();