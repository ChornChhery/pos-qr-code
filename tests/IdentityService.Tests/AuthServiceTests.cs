using FluentAssertions;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.Security;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Tests;

public class AuthServiceTests
{
    private readonly PasswordService _passwords = new();

    // ---- Login ----

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndWritesAuditLog()
    {
        await using var db = TestSupport.NewDb();
        var user = await TestSupport.AddUserAsync(db, _passwords, role: UserRole.Kitchen);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.LoginAsync("alice", "correct-horse-battery", TestSupport.Info);

        result.Succeeded.Should().BeTrue();
        result.Value!.Response.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Response.TokenType.Should().Be("Bearer");
        result.Value.Response.User.Role.Should().Be("KITCHEN");
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var audit = await db.AuditLogs.SingleAsync();
        audit.Action.Should().Be("LOGIN_SUCCESS");
        audit.UserId.Should().Be(user.Id);
        audit.IpAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task Login_IsCaseInsensitiveOnUsername()
    {
        await using var db = TestSupport.NewDb();
        await TestSupport.AddUserAsync(db, _passwords);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.LoginAsync("  ALICE ", "correct-horse-battery", TestSupport.Info);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsUnauthorizedAndAudited()
    {
        await using var db = TestSupport.NewDb();
        var user = await TestSupport.AddUserAsync(db, _passwords);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.LoginAsync("alice", "wrong-password", TestSupport.Info);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ErrorKind.Unauthorized);
        result.Message.Should().Be(AuthService.InvalidCredentialsMessage);

        var audit = await db.AuditLogs.SingleAsync();
        audit.Action.Should().Be("LOGIN_FAILED");
        audit.UserId.Should().Be(user.Id);
        audit.Details.Should().Contain("bad_password").And.NotContain("wrong-password");
    }

    [Fact]
    public async Task Login_WithUnknownUser_UsesSameMessageAndHasNoUserId()
    {
        await using var db = TestSupport.NewDb();
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.LoginAsync("nobody", "whatever-password", TestSupport.Info);

        result.Error.Should().Be(ErrorKind.Unauthorized);
        result.Message.Should().Be(AuthService.InvalidCredentialsMessage);

        var audit = await db.AuditLogs.SingleAsync();
        audit.UserId.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithInactiveUser_IsUnauthorizedWithSameMessage()
    {
        await using var db = TestSupport.NewDb();
        await TestSupport.AddUserAsync(db, _passwords, isActive: false);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.LoginAsync("alice", "correct-horse-battery", TestSupport.Info);

        result.Error.Should().Be(ErrorKind.Unauthorized);
        result.Message.Should().Be(AuthService.InvalidCredentialsMessage);
    }

    // ---- Refresh ----

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessToken()
    {
        await using var db = TestSupport.NewDb();
        await TestSupport.AddUserAsync(db, _passwords);
        var sut = TestSupport.NewAuthService(db, _passwords);
        var login = await sut.LoginAsync("alice", "correct-horse-battery", TestSupport.Info);

        var refreshed = await sut.RefreshAsync(login.Value!.RefreshToken);

        refreshed.Succeeded.Should().BeTrue();
        refreshed.Value!.Response.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_WithGarbageToken_IsUnauthorized()
    {
        await using var db = TestSupport.NewDb();
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.RefreshAsync("garbage");

        result.Error.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Refresh_AfterUserIsDeactivated_IsUnauthorized()
    {
        await using var db = TestSupport.NewDb();
        var user = await TestSupport.AddUserAsync(db, _passwords);
        var sut = TestSupport.NewAuthService(db, _passwords);
        var login = await sut.LoginAsync("alice", "correct-horse-battery", TestSupport.Info);

        user.IsActive = false;
        await db.SaveChangesAsync();

        var result = await sut.RefreshAsync(login.Value!.RefreshToken);

        result.Error.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReflectsRoleChangeMadeAfterLogin()
    {
        await using var db = TestSupport.NewDb();
        var user = await TestSupport.AddUserAsync(db, _passwords, role: UserRole.Cashier);
        var sut = TestSupport.NewAuthService(db, _passwords);
        var login = await sut.LoginAsync("alice", "correct-horse-battery", TestSupport.Info);

        user.Role = UserRole.Manager;
        await db.SaveChangesAsync();

        var result = await sut.RefreshAsync(login.Value!.RefreshToken);

        result.Value!.Response.User.Role.Should().Be("MANAGER");
    }

    // ---- Register ----

    [Fact]
    public async Task Register_CreatesUserWithHashedPasswordAndAuditsTheActor()
    {
        await using var db = TestSupport.NewDb();
        var admin = await TestSupport.AddUserAsync(db, _passwords, "boss", role: UserRole.Admin);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.RegisterAsync(
            admin.Id, new RegisterRequest("  Bob ", "long-enough-password", "kitchen"), TestSupport.Info);

        result.Succeeded.Should().BeTrue();
        result.Value!.Username.Should().Be("bob");
        result.Value.Role.Should().Be("KITCHEN");

        var stored = await db.Users.SingleAsync(u => u.Username == "bob");
        stored.PasswordHash.Should().NotContain("long-enough-password");
        _passwords.Verify(stored.PasswordHash, "long-enough-password").Should().BeTrue();

        var audit = await db.AuditLogs.SingleAsync(a => a.Action == "USER_REGISTERED");
        audit.UserId.Should().Be(admin.Id);
        audit.Details.Should().NotContain("long-enough-password");
    }

    [Fact]
    public async Task Register_WithExistingUsername_IsConflict()
    {
        await using var db = TestSupport.NewDb();
        var admin = await TestSupport.AddUserAsync(db, _passwords, "boss", role: UserRole.Admin);
        await TestSupport.AddUserAsync(db, _passwords, "bob");
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.RegisterAsync(
            admin.Id, new RegisterRequest("BOB", "long-enough-password", "CASHIER"), TestSupport.Info);

        result.Error.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Register_WithUnknownRole_IsValidationError()
    {
        await using var db = TestSupport.NewDb();
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.RegisterAsync(
            Guid.NewGuid(), new RegisterRequest("bob", "long-enough-password", "GUEST"), TestSupport.Info);

        result.Error.Should().Be(ErrorKind.Validation);
        (await db.Users.AnyAsync()).Should().BeFalse();
    }

    // ---- Role changes ----

    [Fact]
    public async Task ChangeRole_UpdatesRoleAndWritesAuditLog()
    {
        await using var db = TestSupport.NewDb();
        var admin = await TestSupport.AddUserAsync(db, _passwords, "boss", role: UserRole.Admin);
        var target = await TestSupport.AddUserAsync(db, _passwords, "bob", role: UserRole.Cashier);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.ChangeRoleAsync(admin.Id, target.Id, "MANAGER", TestSupport.Info);

        result.Succeeded.Should().BeTrue();
        result.Value!.Role.Should().Be("MANAGER");

        var audit = await db.AuditLogs.SingleAsync(a => a.Action == "USER_ROLE_CHANGED");
        audit.UserId.Should().Be(admin.Id);
        audit.Details.Should().Contain("CASHIER").And.Contain("MANAGER");
    }

    [Fact]
    public async Task ChangeRole_OnSelf_IsForbidden()
    {
        await using var db = TestSupport.NewDb();
        var admin = await TestSupport.AddUserAsync(db, _passwords, "boss", role: UserRole.Admin);
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.ChangeRoleAsync(admin.Id, admin.Id, "CASHIER", TestSupport.Info);

        result.Error.Should().Be(ErrorKind.Forbidden);
        (await db.Users.SingleAsync()).Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task ChangeRole_ForUnknownUser_IsNotFound()
    {
        await using var db = TestSupport.NewDb();
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.ChangeRoleAsync(Guid.NewGuid(), Guid.NewGuid(), "MANAGER", TestSupport.Info);

        result.Error.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task ChangeRole_WithUnknownRole_IsValidationError()
    {
        await using var db = TestSupport.NewDb();
        var sut = TestSupport.NewAuthService(db, _passwords);

        var result = await sut.ChangeRoleAsync(Guid.NewGuid(), Guid.NewGuid(), "OWNER", TestSupport.Info);

        result.Error.Should().Be(ErrorKind.Validation);
    }

    // ---- List ----

    [Fact]
    public async Task ListUsers_PaginatesAndClampsPageSize()
    {
        await using var db = TestSupport.NewDb();
        foreach (var name in new[] { "a", "b", "c", "d", "e" })
        {
            await TestSupport.AddUserAsync(db, _passwords, name);
        }

        var sut = TestSupport.NewAuthService(db, _passwords);

        var page2 = await sut.ListUsersAsync(page: 2, pageSize: 2);
        page2.TotalCount.Should().Be(5);
        page2.Items.Select(u => u.Username).Should().Equal("c", "d");

        var clamped = await sut.ListUsersAsync(page: 0, pageSize: 5000);
        clamped.Page.Should().Be(1);
        clamped.PageSize.Should().Be(100);
    }
}
