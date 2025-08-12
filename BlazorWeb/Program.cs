using Blazored.LocalStorage;
using BlazorWeb;
using BlazorWeb.Services;
using GenericHttpClientBase;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// Đăng ký DelegatingHandler để xử lý token
builder.Services.AddTransient<TokenHandler>();

// 1. HttpClient cho xác thực (Login, Register, RefreshToken)
// Không cần đính kèm token tự động vào header
builder.Services.AddHttpClient("Auth", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiGateway:Server1") ?? "https://localhost:7058");
});

// 2. HttpClient cho các API CRUD cần xác thực
// Sử dụng DelegatingHandler để tự động gắn token và xử lý khi token hết hạn
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ApiGateway:Server1") ?? "https://localhost:7058");
}).AddHttpMessageHandler<TokenHandler>(); // Gắn handler vào đây

// Đăng ký service generic
builder.Services.AddScoped(typeof(IGenericApiService<>), typeof(GenericApiService<>));

builder.Services.AddOidcAuthentication(options =>
{
    // Configure your authentication provider options here.
    // For more information, see https://aka.ms/blazor-standalone-auth
    builder.Configuration.Bind("Local", options.ProviderOptions);
});

await builder.Build().RunAsync();