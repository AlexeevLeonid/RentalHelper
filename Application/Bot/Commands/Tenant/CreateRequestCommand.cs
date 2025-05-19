using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using RentalHelper.Domain;
using Application.Services;
using Telegram.Bot.Types.ReplyMarkups;
using DocumentFormat.OpenXml.Bibliography;
namespace Application.Bot.Commands.TenantCommands;

public class CreateRequestCommand : BotCommandBase
{
    private readonly RequestService requestService;
    private readonly RoomService roomService;

    private Dictionary<long, Request> requests = new Dictionary<long, Request>();
    public CreateRequestCommand(AppDbContext context, UserService userService, RequestService requestService, RoomService roomService) : base(userService)
    {
        this.requestService = requestService;
        this.roomService = roomService;
    }

    public override bool CanHandle(string command, uState s, Role role)
    {
        return role == Role.Арендатор && (command == "create_request" || s == uState.TenantCreatingRequest);
    }

    public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
    {
        if (message == null) message = query.Message;
        var userId = message.Chat.Id;
        var tenant = await userService.GetTenantByIdAsync(userId);

        if (tenant.UserState == uState.Idle)
        {
            await userService.SetUserState(tenant, uState.TenantCreatingRequest);
            await botClient.SendMessage(userId, "Введите описание вашей заявки:");
        }
        else if (query == null)
        {
            requests[userId] = new Request()
            {
                Description = message.Text ?? throw new ArgumentException("Пустое сообщение"),
                Status = Status.Новая,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
            };
            await botClient.SendMessage(
                    chatId: userId,
                    text: $"К какому помещению относится запрос",
                    replyMarkup: new InlineKeyboardMarkup(tenant.Rooms.Select(
                        x => new[] { InlineKeyboardButton.WithCallbackData(
                            text: x.Name,
                            callbackData: $"room:{x.Id}" )
                        }.ToArray()))
                    );
        }
        //else if (query.Data.StartsWith("room"))
        //{
        //    var id = int.Parse(query.Data.Split(":")[1]);
        //    requests[userId].RoomId = id;
        //    await botClient.SendMessage(
        //            chatId: userId,
        //            text: $"Выберите приоритет задачи",
        //            replyMarkup: new InlineKeyboardMarkup(Enum.GetNames<Priority>().Select(
        //                x => new[] { InlineKeyboardButton.WithCallbackData(
        //                    text: x,
        //                    callbackData: x )
        //                }.ToArray()))
        //            );
        //}
        else
        {
            var id = int.Parse(query.Data.Split(":")[1]);
            requests[userId].RoomId = id;
            var priority = requests[userId].Description.Contains("труба") ? Priority.Средний : requests[userId].Description.Contains("проводка") ? Priority.Высокий : Priority.Низкий;//Enum.Parse<Priority>(query.Data);
            requests[userId].Priority = priority;
            await requestService.CreateRequest(requests[userId]);
            

            await userService.SetUserState(tenant, uState.Idle);
            var classification = requests[userId].Description.Contains("труба") ? "сантехника" : requests[userId].Description.Contains("проводка") ? "электрика" : "другое";
            await botClient.SendMessage(
                chatId: userId,
                text: $"Заявка отправлена: {requests[userId].Description}\n\nПриоритет: {priority}\n\nЗаявка классифицирована как: {classification}");
            await SendIdleMenu(botClient, userId);
            //var msg = $"Появилась новая заявка: {requests[userId].Description}\n\nПриоритет: {priority.ToString()} \n\n" +
            //            $"Клиент: {tenant.Name}\n\nПомещение: {requests[userId].Room.Name}";
            //if (priority == Priority.Низкий)
            //    foreach (var worker in await userService.GetFreeWorkersAsync())
            //    {
            //        await botClient.SendMessage(worker.TelegramId, msg);
            //    }
            //else
            //{
            //    foreach (var worker in await userService.GetWorkersAsync())
            //    {
            //        await botClient.SendMessage(worker.TelegramId, msg);
            //    }

            //    if (priority == Priority.Высокий)
            //    {
            //        foreach (var admin in await userService.GetAdminsAsync())
            //        {
            //            await botClient.SendMessage(admin.TelegramId, msg);
            //        }
            //    }
            //}
            requests.Remove(userId);
        }
    }
}
