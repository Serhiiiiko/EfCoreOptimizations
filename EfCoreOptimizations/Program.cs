using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Data;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{ });

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost,1433;Database=EfCoreOptimizations;User Id=sa;Password=Q7wa3xSyuKz9muGj9K4JEG2m;TrustServerCertificate=True;";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    // Log SQL queries with parameters
    options.LogTo(
        message => Debug.WriteLine(message),
        new[] { DbLoggerCategory.Database.Command.Name },
        Microsoft.Extensions.Logging.LogLevel.Information
    );
});

// Register data seeder
builder.Services.AddScoped<DataSeeder>();

// Configure Logging with Seq
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddSeq(builder.Configuration.GetSection("Seq"));

// Configure OpenTelemetry
var serviceName = "EfCoreOptimizations";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion,
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, httpRequest) =>
                {
                    activity.SetTag("http.request.path", httpRequest.Path);
                    activity.SetTag("http.request.query", httpRequest.QueryString.ToString());
                };
                options.EnrichWithHttpResponse = (activity, httpResponse) =>
                {
                    activity.SetTag("http.response.status_code", httpResponse.StatusCode);
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                // EF Core instrumentation automatically captures SQL statements
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    var commandText = command.CommandText;
                    if (commandText.Length > 500)
                        commandText = commandText.Substring(0, 500) + "...";
                    
                    activity.SetTag("db.statement", commandText);
                    activity.SetTag("db.operation.name", command.CommandType.ToString());
                };
            })
            .AddSqlClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddSource(serviceName)
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] 
                    ?? "http://localhost:5341/ingest/otlp/v1/traces");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(serviceName)
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] 
                    ?? "http://localhost:5341/ingest/otlp/v1/metrics");
            });
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EF Core Optimizations API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Ensuring database is created...");
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        
        logger.LogInformation("Starting data seeding...");
        var seeder = services.GetRequiredService<DataSeeder>();
        
        // Seed with configurable amounts
        var customerCount = builder.Configuration.GetValue<int>("DataSeeding:CustomerCount", 50000);
        var productsPerCategory = builder.Configuration.GetValue<int>("DataSeeding:ProductsPerCategory", 10000);
        
        await seeder.SeedAsync(customerCount, productsPerCategory);
        logger.LogInformation("Data seeding completed successfully!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();