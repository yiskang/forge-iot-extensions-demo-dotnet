using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Newtonsoft.Json.Serialization;
using JsonFlatFileDataStore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "db.json");
var jsonDataStore = new DataStore(jsonFilePath, keyProperty: "id", reloadBeforeGetCollection: true);

builder.Services.AddSingleton<IDataStore>(jsonDataStore);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    //options.SerializerSettings.Converters.Add(new EmptyStringToNullJsonConverter());
    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
    // options.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ssZ";
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();