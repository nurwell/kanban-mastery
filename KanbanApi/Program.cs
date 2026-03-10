using KanbanApi.Endpoints;
using KanbanApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register services
// BUG: Registering the concrete type only — interface is not mapped
builder.Services.AddSingleton<BoardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapBoardEndpoints();

app.Run();
