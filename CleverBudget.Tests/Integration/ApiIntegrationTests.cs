using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CleverBudget.Core.Common;
using CleverBudget.Core.DTOs;
using CleverBudget.Core.Entities;
using CleverBudget.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CleverBudget.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldReturnToken_ForValidPayload()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var registerDto = BuildRegisterDto(email);

        var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
        Assert.Equal(email, payload.Email);
    }

    [Fact]
    public async Task Login_ShouldReturnToken_AfterRegistration()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var registerDto = BuildRegisterDto(email);

        await client.PostAsJsonAsync("/api/auth/register", registerDto);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(await userManager.CheckPasswordAsync(user!, registerDto.Password));
        }

        var loginDto = new LoginDto
        {
            Email = email,
            Password = registerDto.Password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.Equal(email, auth.Email);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_ForInvalidPassword()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var registerDto = BuildRegisterDto(email);

        await client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = "Wrong123!"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailExists()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var registerDto = BuildRegisterDto(email);

        await client.PostAsJsonAsync("/api/auth/register", registerDto);

        var duplicatedResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);

        Assert.Equal(HttpStatusCode.BadRequest, duplicatedResponse.StatusCode);

        var error = await duplicatedResponse.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions);
        Assert.NotNull(error);
        Assert.False(string.IsNullOrWhiteSpace(error!.Message));
        Assert.Equal("EMAIL_ALREADY_EXISTS", error.ErrorCode);
    }

    [Fact]
    public async Task Profile_ShouldReturnUnauthorized_WhenTokenMissing()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_ShouldReturnUserData_WhenAuthenticated()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var registerDto = BuildRegisterDto(email);

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        Assert.NotNull(auth);

        AttachToken(client, auth!.Token);

        var profileResponse = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>(_jsonOptions);
        Assert.NotNull(profile);
        Assert.Equal(email, profile!.Email);
    }

    [Fact]
    public async Task CompleteUserFlow_ShouldPersistTransaction()
    {
        using var client = CreateClient();
        var email = CreateUniqueEmail();
        var registerDto = BuildRegisterDto(email);

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        Assert.NotNull(auth);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
        }

        AttachToken(client, auth!.Token);

        var categoriesResponse = await client.GetAsync("/api/categories/all");
        categoriesResponse.EnsureSuccessStatusCode();
        var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryResponseDto>>(_jsonOptions);
        Assert.NotNull(categories);
        Assert.NotEmpty(categories!);

        var categoryId = categories!.First().Id;

        var createTransactionDto = new CreateTransactionDto
        {
            Amount = 150.75m,
            Type = TransactionType.Expense,
            Description = "Integration test expense",
            CategoryId = categoryId,
            Date = DateTime.UtcNow
        };

        var transactionResponse = await client.PostAsJsonAsync("/api/transactions", createTransactionDto);
        Assert.Equal(HttpStatusCode.Created, transactionResponse.StatusCode);

        var createdTransaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponseDto>(_jsonOptions);
        Assert.NotNull(createdTransaction);
        Assert.Equal(createTransactionDto.Amount, createdTransaction!.Amount);
        Assert.Equal(createTransactionDto.Description, createdTransaction.Description);
        Assert.Equal(createTransactionDto.Type, createdTransaction.Type);

        var transactionsListResponse = await client.GetAsync("/api/transactions");
        transactionsListResponse.EnsureSuccessStatusCode();

        var pagedResult = await transactionsListResponse.Content.ReadFromJsonAsync<PagedResult<TransactionResponseDto>>(_jsonOptions);
        Assert.NotNull(pagedResult);
        Assert.NotEmpty(pagedResult!.Items);
        Assert.Contains(pagedResult.Items, t => t.Description == createTransactionDto.Description);
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static RegisterDto BuildRegisterDto(string email)
    {
        return new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = "P@ssw0rdA1",
            ConfirmPassword = "P@ssw0rdA1"
        };
    }

    private static string CreateUniqueEmail() => $"user_{Guid.NewGuid():N}@example.com";

    private static void AttachToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private record ErrorResponse(string Message, string? ErrorCode);
}
