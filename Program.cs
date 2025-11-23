using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SummaCore.Configuration;
using System;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ====================================================================

// Agregar controladores
builder.Services.AddControllers();

// Agregar configuración de entorno DGII
builder.Services.AddSingleton<DgiiEnvironmentConfig>();

// Registrar servicios principales
builder.Services.AddScoped<SummaCore.Services.SignerService>();
builder.Services.AddScoped<SummaCore.Services.DgiiService>();

// ====================================================================
// 2. CONFIGURACIÓN DE LOGGING
// ====================================================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddAzureWebAppDiagnostics(); // Para Azure App Service

// ====================================================================
// 3. CONFIGURACIÓN DE CORS (si es necesario para testing)
// ====================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ====================================================================
// 4. CONSTRUIR LA APLICACIÓN
// ====================================================================
var app = builder.Build();

// ====================================================================
// 5. CONFIGURACIÓN DEL PIPELINE HTTP
// ====================================================================

// Página de errores en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Redirección HTTPS
app.UseHttpsRedirection();

// CORS
app.UseCors("AllowAll");

// Autorización
app.UseAuthorization();

// Mapear controladores
app.MapControllers();

// ====================================================================
// 6. ENDPOINTS DE INFORMACIÓN
// ====================================================================

// Endpoint raíz - información del servicio
app.MapGet("/", () => new
{
    servicio = "SummaCore DGII API v1.0",
    descripcion = "API para facturación electrónica DGII - República Dominicana",
    version = "1.0.0",
    framework = ".NET 10 LTS",
    endpoints = new
    {
        emisor = new
        {
            cargaMasiva = "/api/CargaMasiva/procesar-plantilla-dgii",
            consultas = "/api/pruebas/consultar-estatus-lote"
        },
        receptor = new
        {
            recepcion = "/fe/recepcion/api/ecf",
            aprobacionComercial = "/fe/aprobacioncomercial/api/ecf",
            autenticacion = "/fe/autenticacion/api/semilla"
        },
        pruebas = new
        {
            testAutenticacion = "/api/pruebas/test-autenticacion"
        }
    },
    documentacion = "Ver README.md para instrucciones completas"
});

// Health check endpoint
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
});

// ====================================================================
// 7. INICIAR LA APLICACIÓN
// ====================================================================

app.Logger.LogInformation("=================================================");
app.Logger.LogInformation("SummaCore DGII API Iniciado");
app.Logger.LogInformation("Ambiente: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("=================================================");

app.Run();