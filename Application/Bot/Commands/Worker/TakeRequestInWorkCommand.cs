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
using Application.Services;

namespace Application.Bot.Commands.WorkerCommands
{
    public class TakeRequestInWorkCommand : BotCommandBase
    {
        private readonly RequestService requestService;
        public TakeRequestInWorkCommand(AppDbContext context, UserService userSevice, RequestService requestService) : base(userSevice)
        {
            this.requestService = requestService;
        }

        public override bool CanHandle(string command, uState s, Role role)
        {
            return role == Role.Сотрудник && (command == "take_request" || s == uState.WorkerTakeRequest);
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var userId = message.Chat.Id;
            var user = await userService.GetWorkerByIdAsync(userId);



            if (user.UserState == uState.Idle)
            {
                foreach (var v in await requestService.GetRequestsAsync())
                {
                    await botClient.SendMessage(
                    chatId: userId,
                    text: $"Заявка: {v.Description} \n\n Клиента: {v.CreatedBy.Name} \n\nБыла создана: {v.CreatedAt.ToString()} ",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Взять в работу", $"take_in_work:{v.Id}") },
                            })
                    );
                }
                await botClient.SendMessage(
                    chatId: userId,
                    text: $"=====================================",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("отмена", $"cancel") },
                            })
                    );
                await userService.SetUserState(user, uState.WorkerTakeRequest);
                return;
            }
            if (user.UserState == uState.WorkerTakeRequest)
            {
                if (query.Data == "cancel")
                {
                    await userService.SetUserState(user, uState.Idle);
                    await SendIdleMenu(botClient, userId);
                    return;
                }
                var requestId = int.Parse(query.Data.Split(":")[1]);
                var r = await requestService.DoneRequestByIdAsync(requestId);
                await userService.SetUserState(user, uState.Idle);

                await botClient.SendMessage(
                   chatId: userId,
                   text: $"Задача \"{r.Description}\" взята в работу \n\n Клиент: {r.CreatedBy.Name}"
                   );
                await SendIdleMenu(botClient, userId);

                await botClient.SendMessage(
                   chatId: r.CreatedById,
                   text: $"Задача \"{r.Description}\" взята в работу \n\n Работник: {user.Name}"
                   );
                return;
            }
            await userService.SetUserState(user, uState.Idle);
        }
    }
}
