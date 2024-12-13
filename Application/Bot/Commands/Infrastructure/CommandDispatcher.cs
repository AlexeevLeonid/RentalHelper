using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RentalHelper.Domain;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Application.Bot.Commands.Infrastructure;

public class CommandDispatcher
{
    private readonly List<BotCommandBase> _commands;
    private readonly AppDbContext _context;
    private readonly UserService userService;


    public CommandDispatcher(AppDbContext context, IEnumerable<BotCommandBase> commands, UserService userService)
    {
        _context = context;
        _commands = commands.ToList();
        this.userService = userService;
    }

    public async Task HandleMessageAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
    {
        var id = message != null ? message.Chat.Id : query.From.Id;
        var text = message != null ? message.Text : query.Data;
        var user = await userService.GetUserByIdAsync(id);
        var role = user != null ? user.Role : Role.НовыйПользователь;
        var state = user != null ? user.UserState : uState.NewUser;
        foreach (var command in _commands)
        {
            if (command.CanHandle(text, state, role))
            {
                await command.ExecuteAsync(botClient, message, query);
                return;
            }
        }
        await botClient.SendTextMessageAsync(id, "Команда не распознана.");
    }
}
