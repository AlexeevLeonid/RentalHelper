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

namespace Application.Bot.Commands.Tenant
{
    public class BookMeetingRoomCommand : BotCommandBase
    {
        public BookMeetingRoomCommand()
        {
        }

        public override bool CanHandle(string command, uState s)
        {
            return command == "book_meeting_room" || s == uState.TenantBookingRoom;
        } // Перехватывает любое сообщение для контекста

        public override async Task ExecuteAsync(ITelegramBotClient botClient, AppDbContext context, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var user = await context.Users.FirstAsync(x => x.TelegramId == message.Chat.Id);
            var chatId = message.Chat.Id;
            if (user.UserState == uState.Idle)
            {
                var bookedDates = await context.Bookings
                    .Where(b => b.Date >= DateTime.Now.Date)
                    .Select(b => b.Date)
                    .ToListAsync();
                var availableDays = new List<DateTime>();
                var currentDate = DateTime.Now.Date;

                while (availableDays.Count < 10)
                {
                    // Если текущая дата не забронирована, добавляем её в список
                    if (!bookedDates.Contains(currentDate))
                    {
                        availableDays.Add(currentDate);
                    }

                    // Переход к следующему дню
                    currentDate = currentDate.AddDays(1);
                }

                var buttons = availableDays
                    .Where(day => day.DayOfWeek != DayOfWeek.Wednesday) // Убираем среду
                    .Select(day =>
                        new[]{InlineKeyboardButton.WithCallbackData(
                            text: $"{day:dddd, dd.MM}", // Текст на кнопке (например, "Понедельник, 11.12")
                            callbackData: day.ToString("yyyy-MM-dd") // Уникальный идентификатор
                        )}).ToArray();
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"Доступные даты на следующую неделю",
                    replyMarkup: new InlineKeyboardMarkup(buttons)
                    );
                user.UserState = uState.TenantBookingRoom;
                await context.SaveChangesAsync();
                return;
            }
            if (user.UserState == uState.TenantBookingRoom)
            {
                var date = DateTime.ParseExact(
                    query.Data,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture
                );
                await context.Bookings.AddAsync(new Booking()
                {
                    Date = date,
                    UserId = chatId,
                });
                user.UserState = uState.Idle;
                await context.SaveChangesAsync();
                await botClient.SendMessage(
                   chatId: message.Chat.Id,
                   text: $"Переговорка успешно забронирована на {query.Data}"
                   );
                await SendIdleMenu(botClient, message, context);
                return;
            }
            user.UserState = uState.Idle;
            await context.SaveChangesAsync();
        }
    }
}
