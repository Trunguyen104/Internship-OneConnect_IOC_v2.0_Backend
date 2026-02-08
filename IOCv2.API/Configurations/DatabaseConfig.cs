
using IOCv2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.API.Configurations;

public static class DatabaseConfig
{
    public static async Task ApplyMigrations(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            
            var retryCount = 0;
            var maxRetries = 5;

            while (retryCount < maxRetries)
            {
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var initializer = services.GetRequiredService<DbInitializer>();

                    // Using async migration if available, otherwise sync
                    if (context.Database.GetPendingMigrations().Any())
                    {
                        await context.Database.MigrateAsync();
                    }
                    
                    initializer.Initialize();
                    
                    break; 
                }
                catch (Exception ex)
                {
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                        throw;
                    }

                    logger.LogWarning("Database not ready. Retry {Count}/{Max}...", retryCount, maxRetries);
                    await Task.Delay(2000);
                }
            }
        }
    }
}
