using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Warranty.Tests;

public class PaginationAndPolicyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaginationAndPolicyTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()
            ?? doc.RootElement.GetProperty("AccessToken").GetString()
            ?? throw new InvalidOperationException("No accessToken");
    }

    [Fact]
    public async Task Products_Returns_Paged_Shape()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "admin@warranty.local", "Admin@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/products?pageNumber=1&pageSize=5");
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadAsStringAsync();
        Assert.Contains("totalCount", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pageNumber", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customer_Cannot_List_All_Users()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "customer01@warranty.local", "Customer@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/users?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Health_Returns_Success()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
