using Application.Services;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Application.Bot.Commands.AdminCommands
{
    public class ManageRoomCommand : BotCommandBase
    {
        RoomService roomService;
        public ManageRoomCommand(UserService userService, RoomService roomService) : base(userService)
        {
            this.roomService = roomService;
        }
        public override bool CanHandle(string message, uState s, Role role)
        {
            return role == Role.Админ && (message == "assign_room" || s == uState.AdminAssignRoom);
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var chatId = message.Chat.Id;

            var admin = await userService.GetAdminByIdAsync(chatId);
            if (admin.UserState == uState.Idle)
            {
                foreach (var tenant in await userService.GetTenantsAsync())
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Пользователь: {tenant.Name}",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("Назначить комнату", $"assign_room:{tenant.TelegramId}") },
                            new[] { InlineKeyboardButton.WithCallbackData("Отозвать комнату", $"revoke_room:{tenant.TelegramId}") }
                        }));
                await userService.SetUserState(admin, uState.AdminAssignRoom);
            }
            else if (query.Data.StartsWith("assign_room"))
            {
                var data = query.Data.Split(":");
                if (data.Length == 2)
                {
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Выберите свободное помещение",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            (await roomService.GetFreeRoomsAsync()).
                            Select(x => InlineKeyboardButton.WithCallbackData($"Назначить помещение {x.Name}", $"assign_room:{data[1]}:{x.Id}"))
                        }));
                }
                else if (data.Length == 3)
                {
                    var tenant = await userService.GetTenantByIdAsync(long.Parse(data[1]));
                    var room = await roomService.GetRoomByIdAsync(int.Parse(data[2]));
                    await roomService.AssignRoomAsync(tenant, room);
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Пользователю {tenant.Name} успешно прикреплено помещение {room.Name}");
                    await userService.SetUserState(tenant, uState.Idle);
                    await SendIdleMenu(botClient, chatId);

                    await botClient.SendMessage(
                        chatId: tenant.TelegramId,
                        text: $"Вам прикреплено помещение {room.Name}");
                }
            }
            else if (query.Data.StartsWith("revoke_room"))
            {
                var data = query.Data.Split(":");
                if (data.Length == 2)
                {
                    var tenant = await userService.GetTenantByIdAsync(long.Parse(data[1]));
                    foreach (var room in tenant.Rooms)
                        await botClient.SendMessage(
                            chatId: chatId,
                            text: $"Комната: {room.Name}",
                            replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                            new[] { InlineKeyboardButton.WithCallbackData("Отозвать помещение", $"revoke_room:{tenant.TelegramId}:{room.Id}") }
                            }));
                } else
                {
                    var tenant = await userService.GetTenantByIdAsync(long.Parse(data[1]));
                    var room = await roomService.GetRoomByIdAsync(int.Parse(data[2]));
                    await roomService.RevokeRoomAsync(tenant, room);
                    await botClient.SendMessage(
                        chatId: chatId,
                        text: $"Пользователь {tenant.Name} успешно откреплён от помещения {room.Name}");
                    await userService.SetUserState(tenant, uState.Idle);
                    await SendIdleMenu(botClient, chatId);

                    await botClient.SendMessage(
                        chatId: tenant.TelegramId,
                        text: $"От вас откреплена комната {room.Name}");
                }
            }
        }
    }
}
