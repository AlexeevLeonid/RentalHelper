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
using DocumentFormat.OpenXml.InkML;
using System.Globalization;
using Application.Services;

namespace Application.Bot.Commands.TenantCommands
{
    public class BookMeetingRoomCommand : BotCommandBase
    {
        private readonly UserService userService;
        private readonly BookingService bookingService;
        public BookMeetingRoomCommand(UserService userService, BookingService bookingService)
            : base(userService)
        {
            this.userService = userService;
            this.bookingService = bookingService;
        }

        public override bool CanHandle(string command, uState s, Role role)
        {
            return role == Role.Арендатор && (command == "book_meeting_room" || s == uState.TenantBookingRoom);
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient,  Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var userId = message.Chat.Id;
            var tenant = await userService.GetTenantByIdAsync(userId);
            
            if (tenant.UserState == uState.Idle)
            {
                var buttons = (await bookingService.GetAviableDates())
                    .Select(day =>
                        new[]{InlineKeyboardButton.WithCallbackData(
                            text: $"{day:dddd, dd.MM}",
                            callbackData: day.ToString("yyyy-MM-dd")
                        )}).ToArray();

                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Доступные даты на следующую неделю",
                    replyMarkup: new InlineKeyboardMarkup(buttons)
                    );

                await userService.SetUserState(tenant, uState.TenantBookingRoom);
                return;
            }
            if (tenant.UserState == uState.TenantBookingRoom)
            {

                var date = DateTime.ParseExact(
                    query.Data,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture
                );

                await bookingService.BookDateAsync(date, userId);
                await userService.SetUserState(tenant, uState.Idle);

                await botClient.SendMessage(
                   chatId: userId,
                   text: $"Переговорка успешно забронирована на {query.Data}"
                   );

                await SendIdleMenu(botClient, userId);
                return;
            }
            await userService.SetUserState(tenant, uState.Idle);
        }
    }
}
