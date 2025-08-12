using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;

namespace BlazorWeb.Services;

public class TokenHandler(CustomAuthStateProvider tokenService, NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var token = tokenService.GetLocalStorageToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                var tokenRefesh = await tokenService.RefreshTokenAsync();
                if (!string.IsNullOrEmpty(tokenRefesh))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenRefesh);
                }
                else
                {
                    // Xử lý token hết hạn, chuyển hướng người dùng đến trang đăng nhập
                    tokenService.Logout();
                    navigationManager.NavigateTo("/login");
                }
            }
        }
        //Gửi yêu cầu đầu tiên
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Xử lý token hết hạn, chuyển hướng người dùng đến trang đăng nhập
            tokenService.Logout();
            navigationManager.NavigateTo("/login");
        }

        return response;
    }
}