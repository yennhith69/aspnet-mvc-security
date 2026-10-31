using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            if (await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Healthy();

            return HealthCheckResult.Unhealthy("Không thể kết nối cơ sở dữ liệu.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Kiểm tra kết nối cơ sở dữ liệu thất bại.",
                exception: ex);
        }
    }
}
