using Microsoft.EntityFrameworkCore;

using streaming_dotnet.Models;
using streaming_dotnet.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<TestDatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddDbContext<TestDataContext>(opt => opt.UseInMemoryDatabase("Database"));
builder.Services.AddSingleton<TestDataService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*app.UseHttpsRedirection();*/

app.UseAuthorization();

app.MapControllers();

app.Run();
