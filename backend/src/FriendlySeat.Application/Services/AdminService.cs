using FriendlySeat.Application.Common;
using FriendlySeat.Application.Dtos;
using FriendlySeat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FriendlySeat.Application.Services;

public class AdminUserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUserCreateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Moderator";
}

public class AdminManageService
{
    private readonly IAppDbContext _db;
    private readonly IAuditService _audit;

    public AdminManageService(IAppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        return await _db.AdminUsers
            .OrderBy(a => a.Id)
            .Select(a => new AdminUserDto
            {
                Id = a.Id,
                Username = a.Username,
                DisplayName = a.DisplayName,
                Role = a.Role.ToString(),
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt,
                LastLoginAt = a.LastLoginAt
            })
            .ToListAsync(ct);
    }

    public async Task<AdminUserDto> CreateUserAsync(AdminUserCreateRequest request, long operatorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw AppException.BadRequest("username_password_required", "用户名和密码不能为空");

        var exists = await _db.AdminUsers.AnyAsync(a => a.Username == request.Username, ct);
        if (exists) throw AppException.Conflict("username_exists", "用户名已存在");

        if (!Enum.TryParse<AdminRole>(request.Role, true, out var role))
            role = AdminRole.Moderator;

        var admin = new AdminUser
        {
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username : request.DisplayName,
            Role = role,
            Status = EntityStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _db.AdminUsers.Add(admin);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(operatorId, "admin.create", "AdminUser", admin.Id.ToString(), $"创建管理员 {admin.Username}", null, ct);

        return await GetUserAsync(admin.Id, ct) ?? throw AppException.NotFound();
    }

    public async Task<AdminUserDto?> GetUserAsync(long id, CancellationToken ct = default)
    {
        return await _db.AdminUsers
            .Where(a => a.Id == id)
            .Select(a => new AdminUserDto
            {
                Id = a.Id,
                Username = a.Username,
                DisplayName = a.DisplayName,
                Role = a.Role.ToString(),
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt,
                LastLoginAt = a.LastLoginAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task SetStatusAsync(long id, EntityStatus status, long operatorId, CancellationToken ct = default)
    {
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("管理员不存在");
        if (admin.Id == operatorId && status == EntityStatus.Disabled)
            throw AppException.BadRequest("cannot_disable_self", "不能禁用自己");

        admin.Status = status;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "admin.status", "AdminUser", id.ToString(), $"设置管理员 {admin.Username} 状态为 {status}", null, ct);
    }

    public async Task ResetPasswordAsync(long id, string newPassword, long operatorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw AppException.BadRequest("password_required", "新密码不能为空");

        var admin = await _db.AdminUsers.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("管理员不存在");

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(operatorId, "admin.password", "AdminUser", id.ToString(), $"重置管理员 {admin.Username} 密码", null, ct);
    }
}
