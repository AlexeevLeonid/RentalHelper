using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Bot
{
    public class BotBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public BotBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var bot = scope.ServiceProvider.GetRequiredService<HelpDescBot>();
            bot.Start();

            // Предотвращение завершения службы
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
