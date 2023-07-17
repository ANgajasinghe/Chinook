using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Chinook.Services;

public class UserAuthenticationService
{
    // private readonly ChinookContext _context;
    //
    // public UserManagementService(ChinookContext context)
    // {
    //     _context = context;
    // }
    
    
    public string? GetUserId(Task<AuthenticationState>? user)
    {
        var claim = user?.Result?.User;
        var userId = claim?.FindFirst(u => u.Type.Contains(ClaimTypes.NameIdentifier))?.Value;
        return userId;
    }
    
    public bool IsAuthenticated(Task<AuthenticationState>? user)
    {
        var claim = user?.Result?.User;
        return claim?.Identity?.IsAuthenticated ?? false;
    }
}