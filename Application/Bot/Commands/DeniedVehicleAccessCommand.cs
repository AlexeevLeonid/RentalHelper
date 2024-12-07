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

namespace Application.Bot.Commands
{
    public class DeniedVehicleAccessCommand : BotCommandBase
    {
        public DeniedVehicleAccessCommand()
        {
        }

        public override bool CanHandle(string command, uState s)
        {
            return command == "delete_vehicle" || s == uState.TenantDeniyngAccess;
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var user = await context.Users.FirstAsync(x => x.TelegramId == message.Chat.Id);
            var chatId = message.Chat.Id;
            if (user.UserState == uState.Idle)
            {
                foreach (var v in context.Vehicles.Where(x => x.UserId == chatId))
                {
                    await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"{v.PlateNumber}",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("удалить", "vehicle_denied_access") },
                            })
                    );
                }
                user.UserState = uState.TenantDeniyngAccess;
                await context.SaveChangesAsync();
                return;
            }
            if (user.UserState == uState.TenantDeniyngAccess)
            {
                var v = await context.Vehicles.FirstAsync(x => x.PlateNumber == message.Text);
                context.Remove(v);
                user.UserState = uState.Idle;
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                   chatId: message.Chat.Id,
                   text: $"Доступ машине под номером {message.Text} успешно отозван"
                   );
                await SendIdleMenu(botClient, message, context);
                return;
            }
            user.UserState = uState.Idle;
            await context.SaveChangesAsync();
        }
    }
}
