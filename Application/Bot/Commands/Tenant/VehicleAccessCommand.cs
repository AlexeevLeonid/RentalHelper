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
using Application.Services;

namespace Application.Bot.Commands.TenantCommands
{

    public class VehicleAccessCommand : BotCommandBase
    {
        public static Dictionary<long, Vehicle> newVehicles = new();
        private readonly VehicleService vehicleService;

        public VehicleAccessCommand(UserService userService, VehicleService vehicleService) : base(userService)
        {
            this.vehicleService = vehicleService;
        }

        public override bool CanHandle(string message, uState s, Role role)
        {
            return role == Role.Арендатор && (message == "add_vehicle" || s == uState.TenantGivingAccess);
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var userId = message.Chat.Id;
            var user = await userService.GetTenantByIdAsync(userId);
            
            if (user.UserState == uState.Idle)
            {
                userService.SetUserState(user, uState.TenantGivingAccess);

                await botClient.SendMessage(userId, "Введите номер машины");
                newVehicles[userId] = new Vehicle();
                newVehicles[userId].UserId = user.TelegramId;
                return;
            }
            if (user.UserState == uState.TenantGivingAccess)
            {
                if (query == null)
                {
                    newVehicles[userId].PlateNumber = message.Text;
                    await botClient.SendMessage(
                    chatId: userId,
                    text: $"Доступ разовый?",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Да", "vehicle_access_times_one_time") },
                                new[] { InlineKeyboardButton.WithCallbackData("Нет", "vehicle_access_times_many_times") }
                            })
                    );
                    return;
                }
                if (query.Data.StartsWith("vehicle_access_times"))
                {
                    newVehicles[userId].IsOneTime = query.Data == "vehicle_access_times_one_time";
                    await botClient.SendMessage(
                    chatId: userId,
                    text: $"Доступ платный?",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("Да", "vehicle_access_is_paid") },
                                new[] { InlineKeyboardButton.WithCallbackData("Нет", "vehicle_access_is_not_paid") }
                            })
                    );
                    return;
                }
                if (query.Data.StartsWith("vehicle_access_is"))
                {
                    newVehicles[userId].IsPaid = query.Data == "vehicle_access_is_paid";
                    user.UserState = uState.Idle;
                    await vehicleService.AddVehicleAccessAsync(newVehicles[userId]);

                    await botClient.SendMessage(
                    chatId: userId,
                    text: $"Заявка успешно создана, номер {newVehicles[userId].PlateNumber}"
                    );
                    newVehicles.Remove(userId);

                    await SendIdleMenu(botClient, userId);
                }
            }
        }
    }
}
