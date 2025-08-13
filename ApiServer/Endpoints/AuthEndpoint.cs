using Dtos.User;
using EfCoreServices.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiServer.Endpoints;

internal static class AuthEndpoint
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // Nhóm các endpoint dưới một tiền tố chung, ví dụ "/auth"
        var authGroup = app.MapGroup("/auth").WithTags("Auth");

        // Endpoint for user login
        app.MapPost("/login", async ([FromBody] LoginDto dto, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration _configuration) =>
        {
            var result = await signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, false);
            if (!result.Succeeded) return Results.Unauthorized();

            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                var token = await GenerateTokensAsync(user, userManager, _configuration); // Hàm generate token
                return Results.Ok(token);
            }
            else
            {
                return Results.NotFound(new { Message = "User not found" });
            }
        }).WithName("Login");

        // Endpoint for user registration
        app.MapPost("/register", async ([FromBody] RegisterDto dto, UserManager<AppUser> userManager) =>
        {
            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmployeeId = dto.EmployeeId,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, dto.Password);
            if (result.Succeeded)
            {
                // Optionally, you can assign roles or perform additional actions here
                return Results.Ok(new { Message = "User registered successfully" });
            }
            else
            {
                // Return validation errors if registration fails
                return Results.BadRequest(result.Errors);
            }
        }).WithName("Register");

        app.MapPost("refresh", async (string refreshToken, UserManager<AppUser> userManager, IConfiguration _configuration) =>
        {
            var users = await userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var token = await userManager.GetAuthenticationTokenAsync(user, "MyApp", "RefreshToken");
                if (token == refreshToken)
                {
                    return Results.Ok(await GenerateTokensAsync(user, userManager, _configuration));
                }
            }

            return Results.Unauthorized();
        });
    }

    private static async Task<LoginResponse> GenerateTokensAsync(AppUser user, UserManager<AppUser> userManager, IConfiguration _config)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"] ?? "INeverL0ve@W0menBef0reD1@nn@R0$$"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var accessToken = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(15), // Access token hết hạn sau 15 phút
            signingCredentials: creds);

        var refreshToken = await userManager.GenerateUserTokenAsync(user, "RefreshTokenProvider", "RefreshToken");

        return new LoginResponse()
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken),
            RefreshToken = refreshToken,
            ExpiresIn = 900 // 15 phút
        };
    }
}