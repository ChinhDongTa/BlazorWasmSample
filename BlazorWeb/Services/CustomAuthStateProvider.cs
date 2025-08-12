using Blazored.LocalStorage;
using Dtos.User;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BlazorWeb.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient client;
    private readonly ISyncLocalStorageService LocalStorage;

    //todo:còn nhiều lỗi khi hết phiên đăng nhập
    /// <summary>
    /// Constructor for CustomAuthStateProvider.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="LocalStorage"></param>
    public CustomAuthStateProvider(IHttpClientFactory factory, ISyncLocalStorageService LocalStorage)
    {
        client = factory.CreateClient("Auth");
        this.LocalStorage = LocalStorage;
        try
        {
            //var accessToken = LocalStorage.GetItem<string>("accessToken");
            var accessToken = LocalStorage.GetItem<string>("accessToken");
            bool expired = IsExpired();
            if (expired)
                ClearToken();
        }
        catch (Exception ex)
        {
            ClearToken();
            Console.WriteLine(ex.ToString());
        }
    }

    /// <summary>
    /// Is the current authentication token expired?
    /// </summary>
    /// <returns></returns>
    public bool IsExpired()
    {
        return DateTime.Now > LocalStorage.GetItem<DateTime>("expiresIn");
    }

    /// <summary>
    /// Get the current authentication state of the user.
    /// </summary>
    /// <returns></returns>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var infoDto = GetInfoDtoFromLocalStorage();
        if (infoDto != null)
        {
            var user = CreateClaimsPrincipal(infoDto);
            return new AuthenticationState(user);
        }
        else
        {
            try
            {
                var result = await client.GetFromJsonAsync<InfoDto>("");
                if (result != null)
                {
                    SetInfoDto(result);
                    var user = CreateClaimsPrincipal(result);
                    return new AuthenticationState(user);
                }
                else
                {
                    ClearToken();
                    Console.WriteLine($"Error fetching user info");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user info: {ex.Message}");
                ClearToken();
            }
        }

        // Return an unauthenticated state if no valid user information is found
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Login the user with the provided credentials.
    /// </summary>
    /// <param name="loginDto"></param>
    /// <returns></returns>
    public async Task<bool> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var response = await client.PostAsJsonAsync("login", loginDto);
            if (response != null)
            {
                var loginReponse = await response.Content.ReadFromJsonAsync<LoginReponse>();
                if (loginReponse != null)
                {
                    SetAuthenticationTokens(loginReponse);
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during login: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Refresh the authentication token using the refresh token stored in local storage.
    /// </summary>
    /// <returns></returns>
    public async Task<string?> RefreshTokenAsync()
    {
        var refreshToken = LocalStorage.GetItem<string>("refreshToken");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }

        var response = await client.PostAsJsonAsync("refresh", new RefreshTokenRequest(refreshToken));
        if (response != null)
        {
            var loginReponse = await response.Content.ReadFromJsonAsync<LoginReponse>();
            if (loginReponse != null)
            {
                SetAuthenticationTokens(loginReponse);
                return loginReponse.AccessToken;
            }
        }
        ClearToken();
        return null;
    }

    /// <summary>
    /// Logout the user by clearing the authentication tokens and notifying the state change.
    /// </summary>
    public void Logout()
    {
        ClearToken();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Register a new user.
    /// </summary>
    /// <param name="registerDto"></param>
    /// <returns></returns>
    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var responseMessage = await client.PostAsJsonAsync("Register", registerDto);
            if (responseMessage != null)
            {
                return await LoginAsync(new LoginDto
                {
                    Email = registerDto.Email,
                    Password = registerDto.Password
                });
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during registration: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Set authentication tokens in local storage and notify the authentication state change.
    /// </summary>
    /// <param name="response"></param>
    private void SetAuthenticationTokens(LoginReponse response)
    {
        LocalStorage.SetItem("accessToken", response.AccessToken);
        LocalStorage.SetItem("refreshToken", response.RefreshToken);
        LocalStorage.SetItem("expiresIn", DateTime.Now.AddSeconds(response.ExpiresIn));

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Set user information in local storage and notify the authentication state change.
    /// </summary>
    /// <param name="infoDto"></param>
    private void SetInfoDto(InfoDto infoDto)
    {
        LocalStorage.SetItem("Id", infoDto.Id);
        LocalStorage.SetItem("Email", infoDto.Email);
        LocalStorage.SetItem("Username", infoDto.Username);
        LocalStorage.SetItem("RoleNames", infoDto.RoleNames);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Create a ClaimsPrincipal from the provided InfoDto.
    /// </summary>
    /// <param name="infoDto"></param>
    /// <returns></returns>
    private static ClaimsPrincipal CreateClaimsPrincipal(InfoDto infoDto)
    {
        var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, infoDto.Id),
                    new(ClaimTypes.Name, infoDto.Username),
                    new(ClaimTypes.Email, infoDto.Email)
                };

        if (infoDto.RoleNames != null)
        {
            claims.AddRange(infoDto.RoleNames.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, "Token");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Get user information from local storage.
    /// </summary>
    /// <returns></returns>
    private InfoDto? GetInfoDtoFromLocalStorage()
    {
        var id = LocalStorage.GetItem<string>("Id");
        var email = LocalStorage.GetItem<string>("Email");
        var username = LocalStorage.GetItem<string>("Username");
        var roleNames = LocalStorage.GetItem<IEnumerable<string>>("RoleNames");
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(username))
        {
            return null;
        }
        // Kiểm tra định dạng email
        if (!email.Contains('@'))
        {
            return null;
        }
        return new InfoDto
        {
            Id = id,
            Email = email,
            Username = username,
            RoleNames = roleNames
        };
    }

    /// <summary>
    /// Clear the authentication tokens and user information from local storage.
    /// </summary>
    public void ClearToken()
    {
        LocalStorage.RemoveItem("accessToken");
        LocalStorage.RemoveItem("refreshToken");
        LocalStorage.RemoveItem("expiresIn");
        LocalStorage.RemoveItem("Id");
        //LocalStorage.RemoveItem("id");
        //LocalStorage.RemoveItem("infoDtoId");
        //LocalStorage.RemoveItem("jwt_token");
        LocalStorage.RemoveItem("Email");
        LocalStorage.RemoveItem("Username");
        LocalStorage.RemoveItem("RoleNames");
    }

    internal string? GetLocalStorageToken()
    {
        return LocalStorage.GetItemAsString("accessToken");
    }

    /// <summary>
    /// Request model for refreshing the authentication token.
    /// </summary>
    /// <param name="RefreshToken"></param>
    internal record RefreshTokenRequest(string RefreshToken);
}