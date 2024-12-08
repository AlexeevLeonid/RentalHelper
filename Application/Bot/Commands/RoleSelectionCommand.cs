using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands;

public class RoleSelectionCommand : BotCommandBase
{

    public RoleSelectionCommand()
    {
    }

    public override bool CanHandle(string message, uState s, Role r)
    {
        return message == "/start" || message == "Арендатор" || message == "Сотрудник" || message == "Админ";
    }

    public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
    {
        if (message == null) message = query.Message;
        if (!context.Tenants.Any(x => x.TelegramId == message.Chat.Id) || 
            !context.Workers.Any(x => x.TelegramId == message.Chat.Id) || 
            !context.Admins.Any(x => x.TelegramId == message.Chat.Id))
        {
            var text = message.Text;
            if (text == "/start")
            {
                // Отправляем кнопки выбора роли
                var roles = Enum.GetNames<Role>().Select(x => new KeyboardButton(x));
                var keyboard = new ReplyKeyboardMarkup(roles);
                keyboard.OneTimeKeyboard = true;
                keyboard.ResizeKeyboard = true;


                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Выберите вашу роль:",
                    replyMarkup: keyboard
                );
            }
            else
            {
                var role = (Role)Enum.Parse(typeof(Role), text, true);
                if (role == Role.Арендатор)
                {
                    context.Tenants.Add(new RentalHelper.Domain.Tenant
                    {
                        TelegramId = message.Chat.Id,
                        Name = message.Chat.Username ?? "",
                        UserState = uState.Idle,
                        Role = role,
                    });
                } else if (role == Role.Сотрудник)
                {
                    context.Workers.Add(new RentalHelper.Domain.Worker
                    {
                        TelegramId = message.Chat.Id,
                        Name = message.Chat.Username ?? "",
                        UserState = uState.Idle,
                        Role = role,
                    });
                }
                else if (role == Role.Админ)
                {
                    context.Admins.Add(new RentalHelper.Domain.Admin
                    {
                        TelegramId = message.Chat.Id,
                        Name = message.Chat.Username ?? "",
                        UserState = uState.Idle,
                        Role = role,
                    });
                }

                await context.SaveChangesAsync();
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Ваша роль: {role}"
                );
                await SendIdleMenu(botClient, message, context);
            }
        }
        else
        {
            // Если роль уже выбрана, сообщаем об этом
            var user = await context.Tenants.FirstOrDefaultAsync(x => x.TelegramId == message.Chat.Id);
            if (user == null)
                await context.Workers.FirstOrDefaultAsync(x => x.TelegramId == message.Chat.Id);
            if (user == null)
                await context.Admins.FirstOrDefaultAsync(x => x.TelegramId == message.Chat.Id);
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Ваша роль: {user.Role}"
            );
            await SendIdleMenu(botClient, message, context);
        }
    }

}
