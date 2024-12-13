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
using DocumentFormat.OpenXml.Office2010.Excel;
using Application.Services;

namespace Application.Bot.Commands.AdminCommands
{

    public class GetInfoListCommand : BotCommandBase
    {
        private readonly RequestService requestService;
        private readonly VehicleService vehicleService;

        public GetInfoListCommand(UserService userService, RequestService requestService, VehicleService vehicleService) 
            : base(userService)
        {
            this.requestService = requestService;
            this.vehicleService = vehicleService;
        }

        public override bool CanHandle(string message, uState s, Role role)
        {
            return role == Role.Админ && (message == "admin_info" || s == uState.AdminRequestInfo || s == uState.AdminSetPrice);
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient,Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var userId = message.Chat.Id;
            var admin = await userService.GetAdminByIdAsync(userId);
            if (admin.UserState == uState.Idle)
            {
                var tenants = await userService.GetTenantsAsync();
                await TenantRequests(botClient, message, tenants);
                var workers = await userService.GetWorkersAsync();
                await WorkerRequests(botClient, message, workers);
                await userService.SetUserState(admin, uState.Idle);
            }
            else if (query != null && query.Data != null)
            {
                if (query.Data.StartsWith("revoke_request"))
                    await RevokeRequestHandle(botClient, message, query, admin);
                if (query.Data.StartsWith("done_request"))
                    await DoneHandle(botClient, message, query, admin);
                if (query.Data.StartsWith("set_price") || admin.UserState == uState.AdminSetPrice)
                    await SetPriceHandle(botClient, message, query, admin);
                if (query.Data.StartsWith("revoke_access"))
                    await RevokeAccessHandle(botClient,  message, query, admin);
                await SendIdleMenu(botClient, userId);

            }
            else if (message != null)
            {
                if (admin.UserState == uState.AdminSetPrice)
                    await SetPriceHandle(botClient, message, query, admin);
            }
        }

        private static async Task WorkerRequests(ITelegramBotClient botClient, Message message, List<RentalHelper.Domain.Worker> workers)
        {
            foreach (var user in workers.Where(x => x.Role == Role.Сотрудник))
            {
                await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Работник: {user.Name} \n\n Заявки:"
                );
                foreach (var request in user.Requests.Where(x => x.Status != Status.Готово).OrderByDescending(x => x.CreatedAt))
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Завяка: {request.Description} \n\nСоздана: {request.CreatedAt} \n\nСтатус: {request.Status.ToString()}" +
                            ((request.Status == Status.Выполняется) ? $"\n\nАрендатор: {request.CreatedBy.Name}" : ""),
                        replyMarkup: (request.Status == Status.Выполняется) ? new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("Отозвать у работника", $"revoke_request:{request.Id}") },
                            new[] { InlineKeyboardButton.WithCallbackData("Отметить завершённой", $"done_request:{request.Id}") }
                        }
                        ) : null);
                }
            }
        }

        private static async Task TenantRequests(ITelegramBotClient botClient, Message message, List<Tenant> users)
        {
            foreach (var user in users.Where(x => x.Role == Role.Арендатор))
            {
                await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Арендатор: {user.Name} \n\n Заявки:"
                );
                foreach (var request in user.Requests.Where(x => x.Status != Status.Готово).OrderByDescending(x => x.CreatedAt))
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Завяка: {request.Description} \n\nСоздана: {request.CreatedAt} \n\nСтатус: {request.Status.ToString()}" +
                            ((request.Status == Status.Выполняется) ? $"\n\nРаботник: {request.CreatedBy.Name}" : ""),
                        replyMarkup: (request.Status == Status.Выполняется) ? new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("Отозвать у работника", $"revoke_request:{request.Id}") },
                            new[] { InlineKeyboardButton.WithCallbackData("Отметить завершённой", $"done_request:{request.Id}") }
                        }
                        ) : null);
                }
                await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"Арендатор: {user.Name} \n\nДоступ транспорта:"
                );
                foreach (var vehicle in user.Vehicles)
                {
                    await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Номер: {vehicle.PlateNumber} \n\n" +
                        "Оплата: " + (vehicle.IsPaid ? "да\n\n" : "нет\n\n") +
                        "Доступ: " + (vehicle.IsOneTime ? "Одноразовый\n\n" : "Многоразовый\n\n") + 
                        (vehicle.Price == null ? "Цена не выставлена" : $"Цена: {vehicle.Price.Value}"),
                        replyMarkup: (vehicle.Price == null) ? new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("Заполнить цену", $"set_price:{vehicle.Id}") },
                            new[] { InlineKeyboardButton.WithCallbackData("Отозвать доступ", $"revoke_access:{vehicle.Id}") }
                        }
                        ) : null);
                }
            }
        }

        private async Task DoneHandle(ITelegramBotClient botClient, Message message, CallbackQuery query, Admin admin)
        {
            var id = int.Parse(query.Data.Split(":")[1]);
            var request = await requestService.DoneRequestByIdAsync(id);
            await userService.SetUserState(admin, uState.Idle);
            await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Завяка: {request.Description} \n отмечена выполненной");
            if (request.AssignedToId != null)
                await botClient.SendMessage(
                        chatId: request.AssignedToId,
                        text: $"Завяка: {request.Description} \n отмечена выполненной менеджером {admin.Name}");
            await botClient.SendMessage(
                    chatId: request.CreatedById,
                    text: $"Завяка: {request.Description} отмечена выполненной менеджером {admin.Name}" +
                    request.AssignedTo != null ? $" работник {request.AssignedTo.Name}" : "");
            return;
        }

        private async Task RevokeRequestHandle(ITelegramBotClient botClient, Message message, CallbackQuery query, Admin admin)
        {
            var id = int.Parse(query.Data.Split(":")[1]);
            var request = await requestService.RevokeRequestByIdAsync(id);
            await userService.SetUserState(admin, uState.Idle);
            await botClient.SendMessage(
                    chatId: request.AssignedToId ?? throw new Exception("Отозхвали заявку без работника"),
                    text: $"Завяка: {request.Description} \n отозвана у вас менеджером {admin.Name}");
            await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Завяка: {request.Description} отозвана");
            return;
        }

        private async Task RevokeAccessHandle(ITelegramBotClient botClient, Message message, CallbackQuery query, Admin admin)
        {
            var id = int.Parse(query.Data.Split(":")[1]);
            var (userId, plateNumber) = await vehicleService.RevokeVehicleAccessByIdAsync(id);
            await userService.SetUserState(admin, uState.Idle);
            await botClient.SendMessage(
                    chatId: userId,
                    text: $"Доступ машине под номером {plateNumber} отозван менеджером {admin.Name}");
            await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Доступ машине под номером {plateNumber} отозван");
            return;
        }

        private async Task SetPriceHandle(ITelegramBotClient botClient, Message message, CallbackQuery query, Admin admin)
        {
            
            if (admin.UserState == uState.AdminRequestInfo)
            {
                var id = int.Parse(query.Data.Split(":")[1]);
                var vehicle = await vehicleService.GetVehicleByIdAsync(id);
                vehicleService.PrepareVehicle(admin.TelegramId, id);
                await userService.SetUserState(admin, uState.AdminSetPrice);
                await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Установите цену для машины {vehicle.PlateNumber}");
                return;
            }
            else
            {
                var price = int.Parse(message.Text);
                var (pn, ui) = await vehicleService.SetVehiclePrice(admin.TelegramId, price);
                await userService.SetUserState(admin, uState.Idle);
                await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Для машины {pn} установлена цена: {message.Text}");
                await botClient.SendMessage(
                        chatId: ui,
                        text: $"Для машины {pn} установлена цена: {message.Text}");

                await SendIdleMenu(botClient, message.Chat.Id);

            }
        }
    }
}
