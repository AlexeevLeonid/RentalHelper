using Application.Bot;
using Microsoft.Extensions.Hosting;
using System;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
AppDbContext.UseDbContext(builder.Services);
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient("7232513703:AAHbjerRd7AXb83RUVtJdF8yknnKmAHHGS8"));
builder.Services.AddSingleton<HelpDescBot>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var bot = app.Services.GetRequiredService<HelpDescBot>();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    AppDbContext.Seed(context); // Засеивание данных
}
bot.Start();

app.Run();
