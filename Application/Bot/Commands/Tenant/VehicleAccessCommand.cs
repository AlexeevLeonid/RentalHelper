using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using Microsoft.EntityFrameworkCore;

namespace Application.Bot.Commands.Tenant
{

    public class VehicleAccessCommand : BotCommandBase
    {
        public static Dictionary<long, Vehicle> newVehicles = new();
        public VehicleAccessCommand()
        {
        }

        public override bool CanHandle(string message, uState s)
        {
            return message == "add_vehicle" || s == uState.TenantGivingAccess;
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var user = await context.Users.FirstAsync(x => x.TelegramId == message.Chat.Id);
            var chatId = message.Chat.Id;
            if (user.UserState == uState.Idle)
            {
                user.UserState = uState.TenantGivingAccess;
                await context.SaveChangesAsync();
                await botClient.SendMessage(chatId, "Введите номер машины");
                newVehicles[chatId] = new Vehicle();
                newVehicles[chatId].UserId = user.TelegramId;
                return;
            }
            if (user.UserState == uState.TenantGivingAccess)
            {
                if (query == null)
                {
                    newVehicles[chatId].PlateNumber = message.Text;
                    await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Доступ разовый?",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Да", "vehicle_access_one_time") },
                                new[] { InlineKeyboardButton.WithCallbackData("Нет", "vehicle_access_many_times") }
                            })
                    );
                    return;
                }
                if (query.Data == "vehicle_access_one_time" || query.Data == "vehicle_access_many_times")
                {
                    newVehicles[chatId].IsOneTime = query.Data == "vehicle_access_one_time";
                    await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Доступ платный?",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Да", "vehicle_access_is_paid") },
                                new[] { InlineKeyboardButton.WithCallbackData("Нет", "vehicle_access_is_not_paid") }
                            })
                    );
                    return;
                }
                if (query.Data == "vehicle_access_is_paid" || query.Data == "vehicle_access_is_not_paid")
                {
                    newVehicles[chatId].IsPaid = query.Data == "vehicle_access_is_paid";
                    user.UserState = uState.Idle;
                    await context.Vehicles.AddAsync(newVehicles[chatId]);
                    await context.SaveChangesAsync();

                    await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Заявка успешно создана, номер {newVehicles[chatId].PlateNumber}"
                    );
                    newVehicles.Remove(chatId);
                    await SendIdleMenu(botClient, message, context);
                }
            }
        }
    }
}
