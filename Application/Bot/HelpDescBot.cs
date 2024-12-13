using Application.Bot.Commands;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System.Net.Http.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Application.Bot.Commands.Infrastructure;
using Application.Bot.Commands.TenantCommands;
using Application.Bot.Commands.WorkerCommands;
using Application.Bot.Commands.AdminCommands;
namespace Application.Bot;

public class HelpDescBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly CommandDispatcher _dispatcher;

    public HelpDescBot(ITelegramBotClient botClient, CommandDispatcher commandDispatcher )
    {
        _botClient = botClient;

        _dispatcher = commandDispatcher;
    }

    public void Start()
    {
        // StartReceiving для работы в режиме Long Polling
        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: CancellationToken.None
        );

        Console.WriteLine("Бот запущен (Long Polling)");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null)
        {
            await _dispatcher.HandleMessageAsync(botClient, message: update.Message);
        }
        if (update.CallbackQuery != null && update.CallbackQuery.Data != null)
        {
            await _dispatcher.HandleMessageAsync(botClient, query: update.CallbackQuery);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }

    
}

