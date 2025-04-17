using Application.Services;
using RentalHelper.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using ClosedXML.Excel;

namespace Application.Bot.Commands.AdminCommands
{
    public class GetVehicleExcelCommand : BotCommandBase
    {
        private readonly RoomService roomsService;
        private readonly VehicleService vehicleService;

        public GetVehicleExcelCommand(UserService userService, RoomService requestService, VehicleService vehicleService)
            : base(userService)
        {
            this.roomsService = requestService;
            this.vehicleService = vehicleService;
        }

        public override bool CanHandle(string command, uState state, Role role)
        {
            return role == Role.Менеджер && (command == "vehicle_excel");
        }

        public override async Task ExecuteAsync(ITelegramBotClient botClient, Message message = null, CallbackQuery query = null)
        {
            if (message == null) message = query.Message ?? throw new Exception("нет пользователя");
            var chatId = message.Chat.Id;

            var vehicles = await vehicleService.GetVehiclesAsync();
            var filePath = "Vehicles.xlsx";
            CreateExcelFile(vehicles, filePath);

            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await botClient.SendDocument(
                chatId: chatId,
                document: InputFile.FromStream(stream, "example.xlsx")
            );
        }

        private static void CreateExcelFile(List<Vehicle> vehicles, string filePath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Vehicles");

                // Заголовки
                worksheet.Cell(1, 1).Value = "Id";
                worksheet.Cell(1, 2).Value = "Plate Number";
                worksheet.Cell(1, 3).Value = "User Name";
                worksheet.Cell(1, 4).Value = "Price";
                worksheet.Cell(1, 5).Value = "User Id";
                worksheet.Cell(1, 6).Value = "Is Paid";
                worksheet.Cell(1, 7).Value = "Is One Time";

                // Заполняем данные
                for (int i = 0; i < vehicles.Count; i++)
                {
                    var vehicle = vehicles[i];
                    worksheet.Cell(i + 2, 1).Value = vehicle.Id;
                    worksheet.Cell(i + 2, 2).Value = vehicle.PlateNumber;
                    worksheet.Cell(i + 2, 3).Value = vehicle.User?.Name ?? "N/A";
                    worksheet.Cell(i + 2, 4).Value = vehicle.Price.HasValue ? vehicle.Price.Value.ToString() : "N/A";
                    worksheet.Cell(i + 2, 5).Value = vehicle.UserId;
                    worksheet.Cell(i + 2, 6).Value = vehicle.IsPaid ? "Yes" : "No";
                    worksheet.Cell(i + 2, 7).Value = vehicle.IsOneTime ? "Yes" : "No";
                }

                // Стилизация
                var headerRow = worksheet.Range("A1:H1");
                headerRow.Style.Font.Bold = true;
                worksheet.Columns().AdjustToContents();

                // Сохраняем файл
                workbook.SaveAs(filePath);
            }
        }
    }
}
