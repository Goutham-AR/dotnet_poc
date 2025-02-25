using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using streaming_dotnet.Hubs;

using streaming_dotnet.Models;
using streaming_dotnet.Services;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*");
                      });
});
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.Configure<TestDatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.AddDbContext<TestDataContext>(opt => opt.UseInMemoryDatabase("Database"));
builder.Services.AddSingleton<TestDataService>();
builder.Services.AddSingleton<UserService>();
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
app.UseCors(MyAllowSpecificOrigins);
app.MapControllers();
app.MapHub<TestHub>("/test");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
            Path.Combine(builder.Environment.ContentRootPath, "downloads")),
    RequestPath = "/download"
});

app.Run();
