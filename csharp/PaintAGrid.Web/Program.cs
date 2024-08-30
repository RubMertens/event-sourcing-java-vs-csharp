using System.Data;
using Framework;
using PaintAGrid.Web;
using PaintAGrid.Web.Grid;
using PaintAGrid.Web.SqlConnection;
using SimpleMigrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<EventTypeRegistry>();
builder.Services.AddSingleton<IEventTypeRegistry>(p =>
    p.GetRequiredService<EventTypeRegistry>());
builder.Services.AddSingleton<IEventTypeRegistrar>(p =>
    p.GetRequiredService<EventTypeRegistry>());

builder.Services.AddScoped<EventStore>();

builder.Services.AddScoped<MigrationExecution>();

builder.Services.AddScoped<IDbConnectionFactory, NpgSqlConnectionFactory>(_ =>
    new NpgSqlConnectionFactory(
        builder
            .Configuration
            .GetConnectionString("DefaultConnection") ??
        throw new ArgumentException("No Connection string provided")
    )
);

var app = builder.Build();
// Configure the HTTP request pipeline.


app.Services.GetRequiredService<IEventTypeRegistrar>()
    .Register<GridCreated>("GridCreated")
    .Register<PixelColored>("PixelColored")
    .Register<PixelMoved>("PixelMoved")
    ;

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MigrationExecution>()
        .Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", () => { })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();