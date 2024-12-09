﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalHelper.Domain;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Application.Bot.Commands.Infrastructure;

public class CommandDispatcher
{
    private readonly List<BotCommandBase> _commands;
    private readonly IServiceProvider _provider;


    public CommandDispatcher(List<BotCommandBase> commands, IServiceProvider provider)
    {
        _commands = commands;
        _provider = provider;
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
    {
        using (var scope = _provider.CreateScope())
        {
            var id = message != null ? message.Chat.Id : query.From.Id;
            var text = message != null ? message.Text : query.Data;
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            RentalHelper.Domain.User user;
            user = context.Admins.FirstOrDefault(x => x.TelegramId == id);
            if (user == null)
                user = await context.Workers.FirstOrDefaultAsync(x => x.TelegramId == id);
            if (user == null)
                user = await context.Tenants.FirstOrDefaultAsync(x => x.TelegramId == id);
            var state = user != null ? user.UserState : uState.NewUser;
            var role = user != null ? user.Role : Role.НовыйПользователь;
            foreach (var command in _commands)
            {
                if (command.CanHandle(text, state, role))
                {
                    await command.ExecuteAsync(botClient, context, message, query);
                    return;
                }
            }
            await botClient.SendTextMessageAsync(id, "Команда не распознана.");
        }

        
    }
}
