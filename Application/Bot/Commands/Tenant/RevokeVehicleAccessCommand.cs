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

namespace Application.Bot.Commands.TenantCommands
{
    public class RevokeVehicleAccessCommand : BotCommandBase
    {
        private readonly VehicleService vehicleService;
        public RevokeVehicleAccessCommand(UserService userService, VehicleService vehicleService) : base(userService)
        {
            this.vehicleService = vehicleService;
        }

        public override bool CanHandle(string command, uState s, Role role)
        {
            return role == Role.Арендатор && (command == "delete_vehicle" || s == uState.TenantDeniyngAccess);
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var userId = message.Chat.Id;
            var Tenant = await userService.GetTenantByIdAsync(userId);
            
            if (Tenant.UserState == uState.Idle)
            {
                foreach (var v in await vehicleService.GetUserVehiclesByIdAsync(userId))
                {
                    await botClient.SendMessage(
                    chatId: userId,
                    text: $"Машина с номером: {v.PlateNumber}\n\n" + (v.Price.HasValue ? $"Цена: {v.Price}" : "Цена ещё не выставлена"),
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("удалить", $"vehicle_denied_access:{v.Id}") },
                            })
                    );
                }
                await userService.SetUserState(Tenant, uState.TenantDeniyngAccess);
                return;
            }
            if (Tenant.UserState == uState.TenantDeniyngAccess)
            {
                var id = int.Parse(query.Data.Split(":")[1]);
                var a = await vehicleService.RevokeVehicleAccessByIdAsync(id);
                await userService.SetUserState(Tenant, uState.Idle);
                await botClient.SendMessage(
                   chatId: userId,
                   text: $"Доступ машине под номером {a.Item2} успешно отозван"
                   );
                await SendIdleMenu(botClient, userId);
                return;
            }
            await userService.SetUserState(Tenant, uState.Idle);
        }
    }
}
