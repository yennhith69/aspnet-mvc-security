using Microsoft.Data.SqlClient;

// Chặn lỗi kết nối database: trả trang 503 gọn, không lộ chuỗi kết nối hay chi tiết lỗi (mục 23, 24)
public class DatabaseUnavailableMiddleware
{
    // Mã lỗi SQL Server cho tình huống "chưa sẵn sàng": đang restore, chưa recover, không mở được DB, mất kết nối
    private static readonly HashSet<int> UnavailableCodes = new()
        { 2, 53, 64, 233, 258, 921, 926, 927, 4060, 10053, 10054, 10060, 10928, 10929, 40613, -2 };

    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseUnavailableMiddleware> _logger;

    public DatabaseUnavailableMiddleware(RequestDelegate next, ILogger<DatabaseUnavailableMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsDatabaseUnavailable(ex, out var number))
        {
            if (context.Response.HasStarted) throw;

            // Chỉ ghi số lỗi, KHÔNG ghi chuỗi kết nối hay nội dung lỗi gốc ra người dùng
            _logger.LogError("Database không sẵn sàng (SqlException số {Number}) khi xử lý {Path}",
                number, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers["Retry-After"] = "30";
            context.Response.Headers["Cache-Control"] = "no-store";

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"message\":\"Hệ thống tạm thời chưa sẵn sàng. Vui lòng thử lại sau.\"}");
            }
            else
            {
                var traceId = System.Net.WebUtility.HtmlEncode(context.TraceIdentifier);
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(
                    "<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\">" +
                    "<title>Hệ thống tạm thời chưa sẵn sàng</title></head>" +
                    "<body style=\"font-family:sans-serif;max-width:560px;margin:80px auto\">" +
                    "<h1>Hệ thống tạm thời chưa sẵn sàng</h1>" +
                    "<p>Dữ liệu đang được bảo trì hoặc khôi phục. Vui lòng thử lại sau ít phút.</p>" +
                    $"<p>Mã sự cố: {traceId}</p></body></html>");
            }
        }
    }

    private static bool IsDatabaseUnavailable(Exception ex, out int number)
    {
        number = 0;
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SqlException sql &&
                (UnavailableCodes.Contains(sql.Number) || sql.IsTransient ||
                 sql.Message.Contains("transport-level", StringComparison.OrdinalIgnoreCase)))
            {
                number = sql.Number;
                return true;
            }
        }
        return false;
    }
}