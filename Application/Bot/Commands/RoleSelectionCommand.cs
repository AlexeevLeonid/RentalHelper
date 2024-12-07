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

    public override bool CanHandle(string message, uState s)
    {
        return message == "/start" || message == "Арендатор" || message == "Технический специалист" || message == "Суперюзер";
    }

    public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
    {
        if (message == null) message = query.Message;
        if (!context.Users.Any(x => x.TelegramId == message.Chat.Id))
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

                context.Users.Add(new RentalHelper.Domain.User
                {
                    TelegramId = message.Chat.Id,
                    Name = message.Chat.Username ?? "",
                    UserState = uState.Idle,
                    Role = (Role)Enum.Parse(typeof(Role), text, true),
                });
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Ваша роль: {context.Users.First(x => x.TelegramId == message.Chat.Id).Role}"
                );
                await SendIdleMenu(botClient, message, context);
            }
        }
        else
        {
            // Если роль уже выбрана, сообщаем об этом
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Ваша роль: {context.Users.First(x => x.TelegramId == message.Chat.Id).Role}"
            );
            await SendIdleMenu(botClient, message, context);
        }
    }

}
