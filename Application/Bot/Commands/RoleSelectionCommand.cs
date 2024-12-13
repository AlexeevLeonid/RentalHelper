using Application.Services;
using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands;

public class RoleSelectionCommand : BotCommandBase
{
    public RoleSelectionCommand(UserService userService) : base(userService)
    {
    }

    public override bool CanHandle(string message, uState s, Role r)
    {
        return message == "/start" || r == Role.НовыйПользователь;
    }

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
    {
        var userId = message.Chat.Id;
        RentalHelper.Domain.User user = null;
        try
        {
            user = await userService.GetUserByIdAsync(userId);
        }
        catch (ArgumentException ex)
        {
            user = new NewUser()
            {
                Name = message.Chat.Username,
                TelegramId = userId,
                Role = Role.НовыйПользователь,
                UserState = uState.NewUser
            };
            await userService.AddNewUserAsync(user as NewUser);
            await botClient.SendMessage(
                chatId: userId,
                text: $"Регистрация успешна, администратор в скором времени выставит вам роль"
            );
            foreach (var admin in await userService.GetAdminsAsync())
            {
                await botClient.SendMessage(
                chatId: userId,
                text: $"Зарегистрирован новый пользователь, необходимо определить его роль"
            );
            }
        }
        if (user.Role == Role.НовыйПользователь)
        {
            await botClient.SendMessage(
                chatId: userId,
                text: $"Регистрация успешна, администратор в скором времени выставит вам роль"
            );
        }
        else
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Ваша роль: {user.Role}"
            );
            await SendIdleMenu(botClient, userId);
        }
    }

}
