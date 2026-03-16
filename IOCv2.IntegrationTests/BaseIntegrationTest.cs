using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using IOCv2.IntegrationTests.Factories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IOCv2.IntegrationTests;

[Collection("Integration collection")]
public abstract class BaseIntegrationTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    protected readonly HttpClient Client;

    public BaseIntegrationTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        // Create a client that will be used to make requests to the test server
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false //test login statuscode
        });
    }

    /// <summary>
    /// Helper to get a service from the DI container of the test host.
    /// Useful when checking the DB state directly after an API request.
    /// </summary>
    protected internal T? GetService<T>()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetService<T>();
    }

    protected IServiceScope CreateScope()
    {
        return _factory.Services.CreateScope();
    }

    protected internal async Task AuthenticateAsUserAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Authentication failed with status {response.StatusCode}. Content: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResult>();
        
        string accessToken = "";
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var tokenCookie = cookies.FirstOrDefault(c => c.StartsWith("accessToken="));
            if (tokenCookie != null)
            {
                accessToken = tokenCookie.Split(';')[0].Substring("accessToken=".Length);
            }
        }

        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    private class ApiResult
    {
        public LoginResponseInner Data { get; set; } = null!;
    }

    private class LoginResponseInner
    {
        public string AccessToken { get; set; } = null!;
    }

    protected async Task<HttpResponseMessage> PostAsync<T>(string url, T body)
    {
        return await Client.PostAsJsonAsync(url, body);
    }

    protected async Task<HttpResponseMessage> PutAsync<T>(string url, T body)
    {
        return await Client.PutAsJsonAsync(url, body);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        return await Client.DeleteAsync(url);
    }

    // You can add helper methods here, for example:
    // - AuthenticateAsUserAsync(...)
    // - CreateProjectAsync(...)
    // etc.
}
