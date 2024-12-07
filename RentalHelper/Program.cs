using Application.Bot;
using System;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
bot.Start();

app.Run();
