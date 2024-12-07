using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands.Worker
{

    public class DoneRequestCommand : BotCommandBase
    {
        public DoneRequestCommand()
        {
        }

        public override bool CanHandle(string command, uState s)
        {
            return command == "done_request" || s == uState.WorkerDoneRequest;
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var user = await context.Users.FirstAsync(x => x.TelegramId == message.Chat.Id);
            var chatId = message.Chat.Id;
            if (user.UserState == uState.Idle)
            {
                foreach (var v in context.Requests.Where(x => x.AssignedToId == chatId && x.Status == Status.Выполняется).
                    OrderByDescending(x => x.CreatedAt).Include(x => x.CreatedBy))
                {
                    await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Заявка: {v.Description} \n\n Клиента: {v.CreatedBy.Name}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Завершено", $"work_done:{v.Id}") },
                            })
                    );
                }
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"=====================================",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("отмена", $"cancel") },
                            })
                    );
                user.UserState = uState.WorkerDoneRequest;
                await context.SaveChangesAsync();
                return;
            }
            if (user.UserState == uState.WorkerDoneRequest)
            {
                if (query.Data == "cancel")
                {
                    user.UserState = uState.Idle;
                    await context.SaveChangesAsync();
                    await SendIdleMenu(botClient, message, context);
                    return;
                }
                var id = int.Parse(query.Data.Split(":")[1]);
                var r = await context.Requests.FirstAsync(x => x.Id == id);
                r.Status = Status.Готово;
                user.UserState = uState.Idle;
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                   chatId: message.Chat.Id,
                   text: $"Задача \"{r.Description}\" выполнена \n\n Клиент: {r.CreatedBy.Name}"
                   );
                await SendIdleMenu(botClient, message, context);
                await botClient.SendMessage(
                   chatId: chatId,//r.CreatedById,
                   text: $"Задача \"{r.Description}\" выполнена \n\n Работник: {user.Name}"
                   );
                return;
            }
            user.UserState = uState.Idle;
            await context.SaveChangesAsync();
        }
    }
}
