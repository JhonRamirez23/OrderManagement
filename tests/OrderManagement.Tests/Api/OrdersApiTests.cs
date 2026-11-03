using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OrderManagement.Tests.Api;

public sealed class OrdersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid KeyboardId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly HttpClient client;

    public OrdersApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsInitialCatalog()
    {
        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponseModel>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreated()
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            CreateRequest(KeyboardId, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task GetUnknownOrder_ReturnsNotFoundProblemDetails()
    {
        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateOrderWithInvalidData_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            new
            {
                customerEmail = "customer@example.com",
                items = Array.Empty<object>()
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrderWithoutEnoughInventory_ReturnsConflict()
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders",
            CreateRequest(KeyboardId, 11));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static object CreateRequest(Guid productId, int quantity)
    {
        return new
        {
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    productId,
                    quantity
                }
            }
        };
    }

    private sealed record ProductResponseModel(Guid Id, string Name, decimal Price, int Stock);
}
