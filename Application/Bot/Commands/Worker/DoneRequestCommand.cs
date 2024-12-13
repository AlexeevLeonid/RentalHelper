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
using Application.Services;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Application.Bot.Commands.WorkerCommands
{

    public class DoneRequestCommand : BotCommandBase
    {
        private readonly RequestService requestService;
        public DoneRequestCommand(UserService userService, RequestService requestService
            ) : base(userService)
        {
            this.requestService = requestService;
        }

        public override bool CanHandle(string command, uState s, Role role)
        {
            return role == Role.Сотрудник && (command == "done_request" || s == uState.WorkerDoneRequest);
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var userId = message.Chat.Id;
            var worker = await userService.GetWorkerByIdAsync(userId);

            if (worker.UserState == uState.Idle)
            {
                foreach (var v in worker.Requests.Where(x => x.Status == Status.Выполняется).OrderByDescending(x => x.CreatedAt))
                {
                    await botClient.SendMessage(
                    chatId: userId,
                    text: $"Заявка: {v.Description} \n\n Клиента: {v.CreatedBy.Name}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Завершено", $"work_done:{v.Id}") },
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
                await userService.SetUserState(worker, uState.WorkerDoneRequest);
                return;
            }
            if (worker.UserState == uState.WorkerDoneRequest)
            {
                if (query.Data == "cancel")
                {
                    await userService.SetUserState(worker, uState.Idle);
                    await SendIdleMenu(botClient, userId);
                    return;
                }
                var requestId = int.Parse(query.Data.Split(":")[1]);
                var request = await requestService.DoneRequestByIdAsync(requestId);
                await userService.SetUserState(worker, uState.Idle);
                await botClient.SendMessage(
                   chatId: userId,
                   text: $"Задача \"{request.Description}\" выполнена \n\n Клиент: {request.CreatedBy.Name}"
                   );
                await SendIdleMenu(botClient, userId);
                await botClient.SendMessage(
                   chatId: request.CreatedById,
                   text: $"Задача \"{request.Description}\" выполнена \n\n Работник: {worker.Name}"
                   );
                return;
            }
            await userService.SetUserState(worker, uState.Idle);
        }
    }
}
