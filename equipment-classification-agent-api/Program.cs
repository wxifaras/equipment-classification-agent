using Asp.Versioning;
using Azure.Search.Documents.Indexes;
using Azure;
using equipment_classification_agent_api.Models;
using equipment_classification_agent_api.Services;
using Microsoft.Extensions.Options;
using Azure.AI.OpenAI;
using Azure.Search.Documents;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

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

builder.Services.AddOptions<AzureStorageOptions>()
           .Bind(builder.Configuration.GetSection(AzureStorageOptions.AzureStorage))
           .ValidateDataAnnotations();

builder.Services.AddSingleton<IAzureSQLService>(sp =>
{
    var azureSQLOptions = sp.GetRequiredService<IOptions<AzureSQLOptions>>();
    var logger = sp.GetRequiredService<ILogger<AzureSQLService>>();
    return new AzureSQLService(azureSQLOptions, logger);
});
builder.Services.AddSingleton(sp =>
{
    var azureAISearchOptions = sp.GetRequiredService<IOptions<AzureAISearchOptions>>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Initializing Search Index Client with endpoint: {Endpoint}", azureAISearchOptions.Value.SearchServiceEndpoint);
    return new SearchIndexClient(new Uri(azureAISearchOptions.Value.SearchServiceEndpoint!), new AzureKeyCredential(azureAISearchOptions.Value.SearchAdminKey!));
});

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var azureOpenAIOptions = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>();

    logger.LogInformation("Initializing OpenAI Client with endpoint: {Endpoint}", azureOpenAIOptions.Value.AzureOpenAIEndPoint);
    return new AzureOpenAIClient(new Uri(azureOpenAIOptions.Value.AzureOpenAIEndPoint!), new AzureKeyCredential(azureOpenAIOptions.Value.AzureOpenAIKey!));
});

//Register Search Client
builder.Services.AddSingleton(sp =>
{
    var azureAISearchOptions = sp.GetRequiredService<IOptions<AzureAISearchOptions>>();
    var indexClient = sp.GetRequiredService<SearchIndexClient>();
    return indexClient.GetSearchClient(azureAISearchOptions.Value.IndexName);
});

builder.Services.AddSingleton<IAzureAISearchService>(sp =>
{
    var azureAISearchOptions = sp.GetRequiredService<IOptions<AzureAISearchOptions>>();
    var azureOpenAIOptions = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>();
    var logger = sp.GetRequiredService<ILogger<AzureAISearchService>>();
    var azureSqlService = sp.GetRequiredService<IAzureSQLService>();
    var searchIndexClient = sp.GetRequiredService<SearchIndexClient>();
    var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
    var searchClient = sp.GetRequiredService <SearchClient>();
    return new AzureAISearchService(logger, azureAISearchOptions, azureOpenAIOptions, azureSqlService, searchIndexClient, azureOpenAIClient, searchClient);
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