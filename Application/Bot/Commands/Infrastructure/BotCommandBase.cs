using Application.Services;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands;

public abstract class BotCommandBase
{
    protected readonly UserService userService;
    protected IServiceProvider _serviceProvider;
    protected BotCommandBase(UserService userService)
    {
        this.userService = userService;
    }
    public abstract bool CanHandle(string command, uState state, Role role);
    public abstract Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null);

    public async Task SendIdleMenu(ITelegramBotClient botClient, long id)
    {
        await botClient.SendMessage(
                    chatId: id,
                    text: $"Выберите действие",
                    replyMarkup: await GetKeyboardForUserAsync(id)
                );
    }
    public async Task<IReplyMarkup> GetKeyboardForUserAsync(long id)
    {
        var user = await userService.GetUserByIdAsync(id);
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
                    new[] { InlineKeyboardButton.WithCallbackData("Присвоить роль новым пользователям", "assign_role") },
                    new[] { InlineKeyboardButton.WithCallbackData("Управление комнатами", "assign_room") },
                    new[] { InlineKeyboardButton.WithCallbackData("Список допущенных машин", "vehicle_excel") }
                });

                default:
                    return new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Выбрать роль", "/start") }
                });
            }
    }
}
