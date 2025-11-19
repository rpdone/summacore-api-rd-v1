using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1. AGREGAR SERVICIOS AL CONTENEDOR
// Esta línea es vital: busca todos los archivos que terminen en "Controller"
builder.Services.AddControllers(); 

// Registrar tus servicios propios (para inyección de dependencias)
builder.Services.AddScoped<SummaCore.Services.SignerService>();
builder.Services.AddScoped<SummaCore.Services.DgiiService>();

var app = builder.Build();

// 2. CONFIGURAR EL PIPELINE HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// 3. ACTIVAR LAS RUTAS
// Esta línea le dice a la app: "Si alguien llama a una URL, busca en los Controladores"
app.MapControllers(); 

// Mensaje por defecto en la raíz (para saber si la app vive)
app.MapGet("/", () => "¡SummaCore API está corriendo! Usa /api/pruebas/ejecutar-paso2");

app.Run();