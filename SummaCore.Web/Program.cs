using SummaCore.Processor;
using SummaCore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration
var certPath = builder.Configuration["Dgii:CertificatePath"];
var certPass = builder.Configuration["Dgii:CertificatePassword"];

if (string.IsNullOrEmpty(certPath) || string.IsNullOrEmpty(certPass))
{
    // Fallback or warning
    Console.WriteLine("Warning: Certificate configuration missing.");
}

// Register Services
builder.Services.AddSingleton<SignerService>(sp => new SignerService(certPath ?? "", certPass ?? ""));
builder.Services.AddScoped<DgiiService>();
builder.Services.AddScoped<ECFProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment()) // Always enable Swagger for now
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); // For index.html
app.UseAuthorization();

app.MapControllers();

app.Run();
