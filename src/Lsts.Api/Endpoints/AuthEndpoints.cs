using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lsts.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;

namespace Lsts.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            [FromBody] LoginRequest req,
            DbFactory dbFactory,
            IConfiguration cfg,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "username and password are required" });

            var user = await FindUserAsync(dbFactory, req.Username.Trim(), ct);
            if (user is null)
                return Results.Unauthorized();

            if (!PasswordHasher.VerifyPassword(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var token = CreateJwt(user, cfg);
            return Results.Ok(new
            {
                token,
                username = user.Username,
                roles = user.Roles
            });
        })
        .AllowAnonymous();

        group.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var username = principal.Identity?.Name
                           ?? principal.FindFirstValue(ClaimTypes.Name)
                           ?? principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName);

            var roles = principal.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .Distinct()
                .ToArray();

            return Results.Ok(new { username, roles });
        })
        .RequireAuthorization();
    }

    private static async Task<DbUser?> FindUserAsync(
        DbFactory dbFactory,
        string username,
        CancellationToken ct)
    {
        await using var conn = await dbFactory.OpenConnectionAsync(ct);

        long userId;
        string dbUsername;
        string? displayName;
        string passwordHash;

        await using (var cmd = conn.CreateCommand())
        {
            cmd.BindByName = true;
            cmd.CommandText = """
                select
                    u.user_id,
                    u.username,
                    u.display_name,
                    u.password_hash,
                    u.is_active
                from lsts_users u
                where upper(u.username) = upper(:username)
                fetch first 1 rows only
                """;

            cmd.Parameters.Add(new OracleParameter("username", username));

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            var isActive = reader.IsDBNull(4) ? "N" : reader.GetString(4);
            if (!string.Equals(isActive, "Y", StringComparison.OrdinalIgnoreCase))
                return null;

            userId = reader.GetInt64(0);
            dbUsername = reader.GetString(1);
            displayName = reader.IsDBNull(2) ? null : reader.GetString(2);
            passwordHash = reader.IsDBNull(3) ? "" : reader.GetString(3);

            if (string.IsNullOrWhiteSpace(passwordHash))
                return null;
        }

        var roles = new List<string>();

        await using (var rolesCmd = conn.CreateCommand())
        {
            rolesCmd.BindByName = true;
            rolesCmd.CommandText = """
                select r.role_code
                from lsts_user_roles ur
                join lsts_roles r on r.role_id = ur.role_id
                where ur.user_id = :user_id
                  and r.is_active = 'Y'
                order by r.role_code
                """;

            rolesCmd.Parameters.Add(new OracleParameter("user_id", userId));

            await using var rolesReader = await rolesCmd.ExecuteReaderAsync(ct);
            while (await rolesReader.ReadAsync(ct))
            {
                if (!rolesReader.IsDBNull(0))
                    roles.Add(rolesReader.GetString(0));
            }
        }

        return new DbUser(userId, dbUsername, displayName, passwordHash, roles.ToArray());
    }

    private static string CreateJwt(DbUser user, IConfiguration cfg)
    {
        var jwt = cfg.GetSection("Jwt");
        var issuer = jwt["Issuer"]!;
        var audience = jwt["Audience"]!;
        var key = jwt["Key"]!;
        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 480;

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString(CultureInfo.InvariantCulture))
        };

        claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed record DbUser(
        long UserId,
        string Username,
        string? DisplayName,
        string PasswordHash,
        string[] Roles
    );

    public record LoginRequest(string Username, string Password);
}