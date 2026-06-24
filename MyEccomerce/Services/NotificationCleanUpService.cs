using Microsoft.EntityFrameworkCore;
using MyEccomerce.Data;

namespace MyEccomerce.Services
{
    public class NotificationCleanUpService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationCleanUpService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Pag-compute sa petsa (Karon minus 90 ka adlaw)
                        var cutoffDate = DateTime.Now.AddDays(-90);
                        var oldNotifications = context.Notifications.Where(n => n.CreatedAt < cutoffDate);

                        if (await oldNotifications.AnyAsync(stoppingToken))
                        {
                            context.Notifications.RemoveRange(oldNotifications);
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // I-log ang error kung naay dautan nahitabo para ma-trace
                    Console.WriteLine($"CleanUp Service Error: {ex.Message}");
                }

                // Modagan kini pag-usab paglabay sa 24 ka oras (Matag adlaw)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
