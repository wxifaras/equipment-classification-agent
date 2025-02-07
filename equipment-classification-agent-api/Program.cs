using Asp.Versioning;
using equipment_classification_agent_api.Models;
using equipment_classification_agent_api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Load configuration from appsettings.json and appsettings.local.json
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddMvc() // This is needed for controllers
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddOptions<AzureOpenAIOptions>()
           .Bind(builder.Configuration.GetSection(AzureOpenAIOptions.AzureOpenAI))
           .ValidateDataAnnotations();

builder.Services.AddOptions<AzureAISearchOptions>()
           .Bind(builder.Configuration.GetSection(AzureAISearchOptions.AzureAISearch))
           .ValidateDataAnnotations();

builder.Services.AddOptions<AzureSQLOptions>()
           .Bind(builder.Configuration.GetSection(AzureSQLOptions.AzureSQL))
           .ValidateDataAnnotations();

builder.Services.AddSingleton<IAzureSQLService>(sp =>
{
    var azureSQLOptions = sp.GetRequiredService<IOptions<AzureSQLOptions>>();
    var logger = sp.GetRequiredService<ILogger<AzureSQLService>>();
    return new AzureSQLService(azureSQLOptions, logger);
});

builder.Services.AddSingleton<IAzureAISearchService>(sp =>
{
    var azureAISearchOptions = sp.GetRequiredService<IOptions<AzureAISearchOptions>>();
    var azureOpenAIOptions = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>();
    var logger = sp.GetRequiredService<ILogger<AzureAISearchService>>();
    return new AzureAISearchService(azureAISearchOptions, azureOpenAIOptions, logger);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();