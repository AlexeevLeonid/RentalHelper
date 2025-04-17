using Application.Bot;
using Application.Bot.Commands.TenantCommands;
using Application.Bot.Commands;
using Microsoft.Extensions.Hosting;
using System;
using Telegram.Bot;
using Application.Bot.Commands.AdminCommands;
using Application.Bot.Commands.WorkerCommands;
using Application.Bot.Commands.Infrastructure;
using Application.Services;
using Application.Bot.Commands.Worker;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
AppDbContext.UseDbContext(builder.Services);
builder.Services.AddHttpClient();

builder.Services.AddSingleton<ITelegramBotClient>(_ =>
    new TelegramBotClient("7232513703:AAHbjerRd7AXb83RUVtJdF8yknnKmAHHGS8"));
builder.Services.AddHostedService<BotBackgroundService>();
builder.Services.AddScoped<HelpDescBot>();

builder.Services.AddScoped<BotCommandBase, GetInfoListCommand>();
builder.Services.AddScoped<BotCommandBase, RoleSelectionCommand>();
builder.Services.AddScoped<BotCommandBase, BookMeetingRoomCommand>();
builder.Services.AddScoped<BotCommandBase, CreateRequestCommand>();
builder.Services.AddScoped<BotCommandBase, RevokeVehicleAccessCommand>();
builder.Services.AddScoped<BotCommandBase, VehicleAccessCommand>();
builder.Services.AddScoped<BotCommandBase, DoneRequestCommand>();
builder.Services.AddScoped<BotCommandBase, TakeRequestInWorkCommand>();
builder.Services.AddScoped<BotCommandBase, AssignNewUserRoleCommand>(); 
builder.Services.AddScoped<BotCommandBase, ManageRoomCommand>();
builder.Services.AddScoped<BotCommandBase, GetVehicleExcelCommand>();
builder.Services.AddScoped<BotCommandBase, ShowRequestsCommand>();
builder.Services.AddScoped<CommandDispatcher>();

builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<RoomService>();

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

//var bot = app.Services.GetRequiredService<HelpDescBot>();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    AppDbContext.Seed(context); // Засеивание данных
}
//bot.Start();

app.Run();
