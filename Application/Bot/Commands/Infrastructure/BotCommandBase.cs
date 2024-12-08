using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands;

public abstract class BotCommandBase
{
    protected AppDbContext _appDbContext;
    public abstract bool CanHandle(string command, uState state, Role role);
    public abstract Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null);

    public async Task SendIdleMenu(ITelegramBotClient botClient, Message message, AppDbContext context)
    {
        await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Выберите действие",
                    replyMarkup: await GetKeyboardForUserAsync(context, message)
                );
    }
    public async Task<IReplyMarkup> GetKeyboardForUserAsync(AppDbContext context, Message message)
    {
        RentalHelper.Domain.User user;
        user = await context.Admins.FirstOrDefaultAsync(x => x.TelegramId == message.Chat.Id);
        if (user == null)
            await context.Workers.FirstOrDefaultAsync(x => x.TelegramId == message.Chat.Id);
        if (user == null)
            user = await context.Tenants.FirstOrDefaultAsync(x => x.TelegramId == message.Chat.Id);
        if (user == null)
            return new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("старт", "/start") }
                });
        else
            switch (user.Role)
            {
                case Role.Арендатор:
                    return new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Отправить заявку", "create_request") },
                    new[] { InlineKeyboardButton.WithCallbackData("Выдать пропуск транспорту", "add_vehicle") },
                    new[] { InlineKeyboardButton.WithCallbackData("Отозвать пропуск транспорту", "delete_vehicle") },
                    new[] { InlineKeyboardButton.WithCallbackData("Забронировать переговорку", "book_meeting_room") }
                });

                case Role.Сотрудник:
                    return new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Взять заявку в работу", "take_request") },
                                new[] { InlineKeyboardButton.WithCallbackData("Закрыть взятые заявки", "done_request") },
                            });

                case Role.Админ:
                    return new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Сводка по текущему состоянию", "admin_info") },
                    new[] { InlineKeyboardButton.WithCallbackData("Список допущенных машин", "/start") }
                });

                default:
                    return new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Выбрать роль", "/start") },
                    new[] { InlineKeyboardButton.WithCallbackData("Список допущенных машин", "/start") }
                });
            }
        return null;
    }
}
