using Microsoft.Extensions.Diagnostics.HealthChecks;

// Kiểm tra database có sẵn sàng không. Kết quả công khai chỉ là chữ "Healthy" hoặc "Unhealthy", không kèm chi tiết.
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public DatabaseHealthCheck(ApplicationDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}