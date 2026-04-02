
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
            var maxRetries = 10;

            while (retryCount < maxRetries)
            {
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    var initializer = services.GetRequiredService<DbInitializer>();

                    // Ensure database exists before migrating
                    if (context.Database.IsRelational())
                    {
                        await context.Database.MigrateAsync();
                    }
                    else
                    {
                        await context.Database.EnsureCreatedAsync();
                    }
                    
                    await initializer.InitializeAsync();
                    
                    logger.LogInformation("Database migrated and seeded successfully.");
                    break; 
                }
                catch (Exception ex)
                {
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                    {
                        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                        if (ex.InnerException != null)
                        {
                            logger.LogError(ex.InnerException, "Inner Exception: {Message}", ex.InnerException.Message);
                        }
                        throw;
                    }

                    logger.LogWarning("Database not ready (Retry {Count}/{Max}). Error: {Error}. Retrying in 3s...", retryCount, maxRetries, ex.Message);
                    await Task.Delay(3000);
                }
            }
        }
    }
}
