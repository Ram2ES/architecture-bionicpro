using BionicPRO.Api.Configuration;
using BionicPRO.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ClickHouseSettings>(
    builder.Configuration.GetSection(ClickHouseSettings.SectionName));

builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "BionicPRO Reports API",
        Version = "v1",
        Description = "API для получения отчётов",
        Contact = new()
        {
            Name = "BionicPRO Data Team",
            Email = "data@bionicpro.com"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BionicPRO Reports API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Logger.LogInformation("BionicPRO Reports API started");

app.Run();
