namespace IdentityService.Contracts;

public sealed record LoginRequest(string Username, string Password);

/// <param name="Role">One of CASHIER, KITCHEN, MANAGER, ADMIN.</param>
public sealed record RegisterRequest(string Username, string Password, string Role);

/// <param name="Role">One of CASHIER, KITCHEN, MANAGER, ADMIN.</param>
public sealed record ChangeRoleRequest(string Role);
