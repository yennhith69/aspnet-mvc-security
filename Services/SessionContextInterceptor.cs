using System.Data.Common;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class SessionContextInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _http;

    public SessionContextInterceptor(IHttpContextAccessor http) => _http = http;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var cmd = Build(connection);
        cmd.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection,
        ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var cmd = Build(connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand Build(DbConnection connection)
    {
        var (role, donVi, nhanSu) = Resolve();

        var cmd = connection.CreateCommand();
        cmd.CommandText =
            "EXEC sp_set_session_context N'Role', @role, @read_only = 1; " +
            "EXEC sp_set_session_context N'DonViId', @donvi, @read_only = 1; " +
            "EXEC sp_set_session_context N'NhanSuId', @nhansu, @read_only = 1;";
        Add(cmd, "@role", role);
        Add(cmd, "@donvi", donVi);
        Add(cmd, "@nhansu", nhanSu);
        return cmd;
    }

    private (string Role, string? DonVi, string? NhanSu) Resolve()
    {
        var ctx = _http.HttpContext;
        if (ctx == null) return ("System", null, null);

        var user = ctx.User;
        if (user.Identity?.IsAuthenticated != true) return ("Public", null, null);

        var role = new[] { Roles.Admin, Roles.Hr, Roles.Manager, Roles.Lecturer }
            .FirstOrDefault(r => user.IsInRole(r)) ?? "None";
        return (role, user.FindFirstValue("DonViId"), user.FindFirstValue("NhanSuId"));
    }

    private static void Add(DbCommand cmd, string name, string? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = (object?)value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }
}