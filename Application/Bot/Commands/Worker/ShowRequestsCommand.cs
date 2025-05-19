using Application.Services;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using DocumentFormat.OpenXml.Spreadsheet;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands.Worker
{
    public class ShowRequestsCommand : BotCommandBase
    {
        private readonly RequestService requestService;
        public ShowRequestsCommand(UserService userService, RequestService requestService
            ) : base(userService)
        {
            this.requestService = requestService;
        }
        public override bool CanHandle(string command, uState s, Role role)
        {
            return role == Role.Сотрудник && (command == "show_request");
        }
        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var requests = await requestService.GetRequestsAsync();

            foreach (var v in requests) 
            {
                var classification = v.Description.Contains("труба") ? "сантехника" : v.Description.Contains("проводка") ? "электрика" : "другое";
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Заявка: {v.Description} \n\nКлиента: {v.CreatedBy.Name}\n\nСтатус: {v.Status.ToString()} \n\nПомещение: {v.Room.Name}\n\n" +
                    $"Заявка классифицирована как: {classification}"
                    );
            }
        }
    }
}
