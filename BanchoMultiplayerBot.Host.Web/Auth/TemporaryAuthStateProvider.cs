using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace BanchoMultiplayerBot.Host.Web.Auth;

/// <summary>
/// This is 'temporary' and I cannot promise on security while using this, the other part of the authentication takes part in
/// MainLayout.razor.
/// </summary>
public class TemporaryAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal CurrentUser { get; set; }

    public TemporaryAuthStateProvider()
    {
        CurrentUser = CreateAnonymousUser();
    }
    
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(this.CurrentUser));
    }

    private ClaimsPrincipal CreateAnonymousUser()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Sid, "0"),
            new Claim(ClaimTypes.Name, "Anonymous"),
            new Claim(ClaimTypes.Role, "Anonymous")
        }, null);

        return new ClaimsPrincipal(identity);
    }
        
    public Task<AuthenticationState> Authenticate(string username, string id, string role)
    {
        CurrentUser = CreateFakeUser(username, id, role);
        
        var task = GetAuthenticationStateAsync();
        
        NotifyAuthenticationStateChanged(task);
        
        return task;
    }
    
    private ClaimsPrincipal CreateFakeUser(string userName, string id, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Sid, id),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role)
        }, "Authentication type");
        
        return new ClaimsPrincipal(identity);
    }
}