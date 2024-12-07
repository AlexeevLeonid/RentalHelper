using Microsoft.EntityFrameworkCore;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Application.Bot.Commands.Worker
{
    public class TakeRequestInWorkCommand : BotCommandBase
    {
        public TakeRequestInWorkCommand()
        {
        }

        public override bool CanHandle(string command, uState s)
        {
            return command == "take_request" || s == uState.WorkerTakeRequest;
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var user = await context.Users.FirstAsync(x => x.TelegramId == message.Chat.Id);
            var chatId = message.Chat.Id;
            if (user.UserState == uState.Idle)
            {
                foreach (var v in context.Requests.Where(x => x.Status == Status.Новая).OrderByDescending(x => x.CreatedAt).Include(x => x.CreatedBy))
                {
                    await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Заявка: {v.Description} \n\n Клиента: {v.CreatedBy.Name} \n\nБыла создана: {v.CreatedAt.ToString()} ",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Взять в работу", $"take_in_work:{v.Id}") },
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
                user.UserState = uState.WorkerTakeRequest;
                await context.SaveChangesAsync();
                return;
            }
            if (user.UserState == uState.WorkerTakeRequest)
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
                r.Status = Status.Выполняется;
                r.AssignedToId = user.TelegramId;
                user.UserState = uState.Idle;
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                   chatId: message.Chat.Id,
                   text: $"Задача \"{r.Description}\" взята в работу \n\n Клиент: {r.CreatedBy.Name}"
                   );
                await SendIdleMenu(botClient, message, context);
                await botClient.SendMessage(
                   chatId: chatId,//r.CreatedById,
                   text: $"Задача \"{r.Description}\" взята в работу \n\n Работник: {user.Name}"
                   );
                return;
            }
            user.UserState = uState.Idle;
            await context.SaveChangesAsync();
        }
    }
}
