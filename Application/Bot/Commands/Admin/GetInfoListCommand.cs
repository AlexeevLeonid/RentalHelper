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

namespace Application.Bot.Commands.Admin
{

    public class GetInfoListCommand : BotCommandBase
    {
        private static Dictionary<long, int> vehicles = new Dictionary<long, int>();
        public GetInfoListCommand()
        {
        }

        public override bool CanHandle(string message, uState s, Role role)
        {
            return role == Role.Админ && (message == "admin_info" || s == uState.AdminRequestInfo || s == uState.AdminSetPrice);
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var chatId = message.Chat.Id;
            var admin = await context.Admins.FirstAsync(x => x.TelegramId == message.Chat.Id);
            if (admin.UserState == uState.Idle)
            {
                var tenants = context.Tenants.Include(x => x.Requests).Include(x => x.Vehicles).Include(x => x.Bookings).ToList();
                await TenantRequests(botClient, message, tenants);
                var workers = context.Workers.Include(x => x.Requests).ToList();
                await WorkerRequests(botClient, message, workers);
                admin.UserState = uState.AdminRequestInfo;
                await context.SaveChangesAsync();
            }
            else if (query != null && query.Data != null)
            {
                if (query.Data.StartsWith("revoke_request"))
                    await RevokeRequestHandle(botClient, context, message, query, admin);
                if (query.Data.StartsWith("done_request"))
                    await DoneHandle(botClient, context, message, query, admin);
                if (query.Data.StartsWith("set_price") || admin.UserState == uState.AdminSetPrice)
                    await SetPriceHandle(botClient, context, message, query, admin);
                if (query.Data.StartsWith("revoke_access"))
                    await RevokeAccessHandle(botClient, context, message, query, admin);
            } else if (message != null)
            {
                if (admin.UserState == uState.AdminSetPrice)
                    await SetPriceHandle(botClient, context, message, query, admin);
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

        private static async Task TenantRequests(ITelegramBotClient botClient, Message message, List<RentalHelper.Domain.Tenant> users)
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

        private static async Task DoneHandle(ITelegramBotClient botClient, AppDbContext context, Message message, CallbackQuery query, RentalHelper.Domain.Admin admin)
        {
            var id = int.Parse(query.Data.Split(":")[1]);

            var request = context.Requests.Include(x => x.AssignedTo).First(x => x.Id == id);
            request.Status = Status.Готово;
            admin.UserState = uState.Idle;
            await context.SaveChangesAsync();
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

        private static async Task RevokeRequestHandle(ITelegramBotClient botClient, AppDbContext context, Message message, CallbackQuery query, RentalHelper.Domain.Admin admin)
        {
            var id = int.Parse(query.Data.Split(":")[1]);

            var request = context.Requests.First(x => x.Id == id);
            var workerId = request.AssignedToId.Value;
            request.AssignedToId = null;
            request.Status = Status.Новая;
            admin.UserState = uState.Idle;
            await context.SaveChangesAsync();
            await botClient.SendMessage(
                    chatId: workerId,
                    text: $"Завяка: {request.Description} \n отозвана у вас менеджером {admin.Name}");
            await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Завяка: {request.Description} отозвана");
            return;
        }

        private static async Task RevokeAccessHandle(ITelegramBotClient botClient, AppDbContext context, Message message, CallbackQuery query, RentalHelper.Domain.Admin admin)
        {
            var id = int.Parse(query.Data.Split(":")[1]);

            var vehicle = context.Vehicles.First(x => x.Id == id);
            var userId = vehicle.UserId;
            var plateumber = vehicle.PlateNumber;
            context.Vehicles.Remove(vehicle);
            await context.SaveChangesAsync();
            await botClient.SendMessage(
                    chatId: userId,
                    text: $"Доступ машине под номером {plateumber} отозван менеджером {admin.Name}");
            await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Доступ машине под номером {plateumber} отозван");
            return;
        }

        private static async Task SetPriceHandle(ITelegramBotClient botClient, AppDbContext context, Message message, CallbackQuery query, RentalHelper.Domain.Admin admin)
        {
            
            if (admin.UserState == uState.AdminRequestInfo)
            {
                var id = int.Parse(query.Data.Split(":")[1]);
                var vehicle = context.Vehicles.First(x => x.Id == id);
                admin.UserState = uState.AdminSetPrice;
                vehicles[admin.TelegramId] = id;
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Установите цену для машины {vehicle.PlateNumber}");
                return;
            }
            else
            {
                var vehicle = context.Vehicles.First(x => x.Id == vehicles[admin.TelegramId]);
                vehicles.Remove(admin.TelegramId);
                vehicle.Price = int.Parse(message.Text);
                admin.UserState = uState.Idle;
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: $"Для машины {vehicle.PlateNumber} установлена цена: {message.Text}");
                await botClient.SendMessage(
                        chatId: vehicle.UserId,
                        text: $"Для машины {vehicle.PlateNumber} установлена цена: {message.Text}");
            }
        }
    }
}
