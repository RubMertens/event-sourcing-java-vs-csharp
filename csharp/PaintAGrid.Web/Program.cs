using Framework;
using Framework.EventSerialization;
using Framework.SqlConnection;
using PaintAGrid.Web;
using PaintAGrid.Web.Grid;
using PaintAGrid.Web.Grid.Identity;

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
builder.Services.AddScoped<GridIdentityGenerator>();

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


app.Services
    .GetRequiredService<IEventTypeRegistrar>()
    .RegisterAllInAssemblyOf<Program>()
    ;


using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MigrationExecution>()
        .Migrate();
    await scope.ServiceProvider.GetRequiredService<EventStore>().Init();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/grids/{id}", async (EventStore eventStore, int id) =>
{
    var grid = eventStore.AggregateStreamFromSnapshot<GridAggregate>(GridAggregate.StreamIdFromId(id));
    return grid;
});

app.MapGet("/grids",
    async (EventStore store) => { throw new NotImplementedException(); });

app.MapPost("/grids", async (
    CreateGrid createGrid,
    EventStore store,
    GridIdentityGenerator identityGenerator
) =>
{
    var grid = new GridAggregate(
        await identityGenerator.GetNext(),
        createGrid.Name,
        createGrid.Width,
        createGrid.Height
    );
    await store.Store(grid);
    return grid;
});

app.MapPost("/grids/{id}/color",
    async (int id, ColorPixel pixel, EventStore store) =>
    {
        var grid =
            await store.AggregateStreamFromSnapshot<GridAggregate>(
                GridAggregate.StreamIdFromId(id));
        grid.ColorPixel(pixel.x, pixel.y, pixel.color);
        await store.Store(grid);
        return grid;
    });

app.MapPost("/grids/{id}/move",
    async (int id, MovePixel move, EventStore store) =>
    {
        var grid = await store.AggregateStreamFromSnapshot<GridAggregate>(GridAggregate.StreamIdFromId(id));
        grid.MovePixel(move.x, move.y, move.deltaX, move.deltaY);
        await store.Store(grid);
        return grid;
    });


app.Run();

namespace PaintAGrid.Web
{
    record CreateGrid(int Width, int Height, string Name);

    record ColorPixel(int x, int y, string color);

    record MovePixel(int x, int y, int deltaX, int deltaY);
}