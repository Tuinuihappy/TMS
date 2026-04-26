using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Tms.Orders.Application.Features.CreateOrder;
using Tms.Orders.Application.Features.GetOrders;
using Tms.SharedKernel.Application;
using Tms.WebApi.IntegrationTests.Infrastructure;
using Xunit;

namespace Tms.WebApi.IntegrationTests.Endpoints;

public class OrderEndpointsTests : BaseIntegrationTest
{
    public OrderEndpointsTests(TmsWebApplicationFactory factory) : base(factory) { }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object ValidCreateRequest(string? orderNumber = null) => new
    {
        CustomerId = IntegrationTestData.CustomerId,
        OrderNumber = orderNumber ?? $"ORD-{Guid.NewGuid():N}".Substring(0, 12),
        Stops = new[]
        {
            new
            {
                PickupAddress = new
                {
                    Street = "123 Test St", SubDistrict = "Lat Krabang",
                    District = "Lat Krabang", Province = "BKK", PostalCode = "10520"
                },
                DropoffAddress = new
                {
                    Street = "456 Test Ave", SubDistrict = "Bang Na",
                    District = "Bang Na", Province = "BKK", PostalCode = "10260"
                },
                Items = new[] { new { Description = "Package A", WeightKg = 10.0m, VolumeCBM = 0.5m, Quantity = 1 } }
            }
        }
    };

    private async Task<Guid> CreateOrderAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/orders", ValidCreateRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        return body!.Id;
    }

    // ── GET /api/orders ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_ShouldReturnPagedResult()
    {
        var response = await Client.GetAsync("/api/orders");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrders_WithStatusFilter_ShouldReturnOk()
    {
        var response = await Client.GetAsync("/api/orders?status=Draft");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result!.Items.Should().OnlyContain(o => o.Status == "Draft");
    }

    [Fact]
    public async Task GetOrders_WithCustomerIdFilter_ShouldOnlyReturnThatCustomersOrders()
    {
        await CreateOrderAsync();

        var response = await Client.GetAsync($"/api/orders?customerId={IntegrationTestData.CustomerId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result!.Items.Should().OnlyContain(o => o.CustomerId == IntegrationTestData.CustomerId);
    }

    [Fact]
    public async Task GetOrders_Pagination_ShouldRespectPageSizeParam()
    {
        // Ensure at least 2 orders exist
        await CreateOrderAsync();
        await CreateOrderAsync();

        var response = await Client.GetAsync("/api/orders?page=1&pageSize=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result!.Items.Should().HaveCount(1);
        result.PageSize.Should().Be(1);
    }

    // ── GET /api/orders/{id} ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderById_WhenExists_ShouldReturnOrderDetails()
    {
        var id = await CreateOrderAsync();

        var response = await Client.GetAsync($"/api/orders/{id}");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(id);
        order.CustomerId.Should().Be(IntegrationTestData.CustomerId);
        order.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task GetOrderById_WhenNotFound_ShouldReturn404()
    {
        var response = await Client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/orders ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_ShouldReturn201WithOrderDetails()
    {
        var response = await Client.PostAsJsonAsync("/api/orders", ValidCreateRequest("ORD-TEST-001"));
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, content);

        var body = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Status.Should().Be("Draft");
        body.TotalWeight.Should().Be(10.0m);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(body.Id.ToString());
    }

    [Fact]
    public async Task CreateOrder_WithNoStops_ShouldReturn400()
    {
        var req = new
        {
            CustomerId = IntegrationTestData.CustomerId,
            OrderNumber = "ORD-INVALID",
            Stops = Array.Empty<object>()
        };

        var response = await Client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyCustomerId_ShouldReturn400()
    {
        var req = new
        {
            CustomerId = Guid.Empty,
            OrderNumber = "ORD-INVALID",
            Stops = new[]
            {
                new
                {
                    PickupAddress = new { Street = "A", SubDistrict = "B", District = "C", Province = "BKK", PostalCode = "10000" },
                    DropoffAddress = new { Street = "A", SubDistrict = "B", District = "C", Province = "BKK", PostalCode = "10000" },
                    Items = new[] { new { Description = "X", WeightKg = 1.0m, VolumeCBM = 0.1m, Quantity = 1 } }
                }
            }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithZeroWeightItem_ShouldReturn400()
    {
        var req = new
        {
            CustomerId = IntegrationTestData.CustomerId,
            OrderNumber = "ORD-INVALID",
            Stops = new[]
            {
                new
                {
                    PickupAddress = new { Street = "A", SubDistrict = "B", District = "C", Province = "BKK", PostalCode = "10000" },
                    DropoffAddress = new { Street = "D", SubDistrict = "E", District = "F", Province = "BKK", PostalCode = "10000" },
                    Items = new[] { new { Description = "Bad Item", WeightKg = 0.0m, VolumeCBM = 0.1m, Quantity = 1 } }
                }
            }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidPickupWindowOrder_ShouldReturn400()
    {
        var now = DateTime.UtcNow;
        var req = new
        {
            CustomerId = IntegrationTestData.CustomerId,
            OrderNumber = "ORD-INVALID-WIN",
            Stops = new[]
            {
                new
                {
                    PickupAddress = new { Street = "A", SubDistrict = "B", District = "C", Province = "BKK", PostalCode = "10000" },
                    DropoffAddress = new { Street = "D", SubDistrict = "E", District = "F", Province = "BKK", PostalCode = "10000" },
                    Items = new[] { new { Description = "Item", WeightKg = 5.0m, VolumeCBM = 0.2m, Quantity = 1 } },
                    PickupWindow = new { From = now.AddHours(2), To = now.AddHours(1) } // To < From → invalid
                }
            }
        };

        var response = await Client.PostAsJsonAsync("/api/orders", req);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/orders/{id}/confirm ──────────────────────────────────────────

    [Fact]
    public async Task ConfirmOrder_ShouldReturn204()
    {
        var id = await CreateOrderAsync();

        var response = await Client.PutAsync($"/api/orders/{id}/confirm", null);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, content);
    }

    [Fact]
    public async Task ConfirmOrder_WhenNotFound_ShouldReturn404()
    {
        var response = await Client.PutAsync($"/api/orders/{Guid.NewGuid()}/confirm", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/orders/{id}/amend ────────────────────────────────────────────

    [Fact]
    public async Task AmendOrder_Priority_ShouldReturn204()
    {
        var id = await CreateOrderAsync();

        var req = new { Priority = "Express", Notes = "Updated via integration test" };
        var response = await Client.PutAsJsonAsync($"/api/orders/{id}/amend", req);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, content);

        // Verify the update was persisted
        var getResponse = await Client.GetAsync($"/api/orders/{id}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.Priority.Should().Be("Express");
    }

    [Fact]
    public async Task AmendOrder_StopAddress_ShouldReturn204()
    {
        var id = await CreateOrderAsync();

        // Get the stop ID first
        var getResponse = await Client.GetAsync($"/api/orders/{id}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        var stopId = order!.Stops.First().StopId;

        var req = new
        {
            StopId = stopId,
            PickupAddress = new { Street = "999 Amended St", SubDistrict = "New Sub", District = "New Dist", Province = "CNX", PostalCode = "50000" }
        };
        var response = await Client.PutAsJsonAsync($"/api/orders/{id}/amend", req);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, content);
    }

    [Fact]
    public async Task AmendOrder_WhenNotFound_ShouldReturn404()
    {
        var req = new { Priority = "High" };
        var response = await Client.PutAsJsonAsync($"/api/orders/{Guid.NewGuid()}/amend", req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/orders/{id}/cancel ───────────────────────────────────────────

    [Fact]
    public async Task CancelOrder_ShouldReturn204()
    {
        var id = await CreateOrderAsync();

        var req = new { Reason = "Customer requested cancellation" };
        var response = await Client.PutAsJsonAsync($"/api/orders/{id}/cancel", req);
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, content);

        // Verify status changed
        var getResponse = await Client.GetAsync($"/api/orders/{id}");
        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelOrder_WhenNotFound_ShouldReturn404()
    {
        var req = new { Reason = "No longer needed" };
        var response = await Client.PutAsJsonAsync($"/api/orders/{Guid.NewGuid()}/cancel", req);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/orders/import ───────────────────────────────────────────────

    [Fact]
    public async Task ImportOrders_WithValidCsv_ShouldReturn200WithResult()
    {
        var csv = new StringBuilder();
        csv.AppendLine("CustomerId,Priority,PickupStreet,PickupSubDistrict,PickupDistrict,PickupProvince,PickupPostalCode,DropoffStreet,DropoffSubDistrict,DropoffDistrict,DropoffProvince,DropoffPostalCode,ItemDescription,WeightKg,VolumeCBM,Quantity");
        csv.AppendLine($"{IntegrationTestData.CustomerId},Normal,123 Import St,Lat Krabang,Lat Krabang,BKK,10520,456 Dest Ave,Bang Na,Bang Na,BKK,10260,Imported Box,15.0,0.8,2");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv.ToString()));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "orders.csv");

        var response = await Client.PostAsync("/api/orders/import", content);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        var result = await response.Content.ReadFromJsonAsync<Tms.Orders.Application.Features.ImportOrder.ImportOrdersResult>();
        result.Should().NotBeNull();
        result!.TotalRows.Should().Be(1);
    }

    [Fact]
    public async Task ImportOrders_WithNoFile_ShouldReturn400()
    {
        // Multipart form with no file field — IFormFile? resolves to null and triggers BadRequest
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("ignored"), "notAFile");

        var response = await Client.PostAsync("/api/orders/import", content);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("No file uploaded");
    }

    [Fact]
    public async Task ImportOrders_WithWrongFileExtension_ShouldReturn400()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("not a csv"u8.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "orders.txt");

        var response = await Client.PostAsync("/api/orders/import", content);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        body.Should().Contain("Unsupported file format");
    }

    [Fact]
    public async Task ImportOrders_WithInvalidCustomerId_ShouldReturn200WithRowErrors()
    {
        var csv = new StringBuilder();
        csv.AppendLine("CustomerId,Priority,PickupStreet,PickupSubDistrict,PickupDistrict,PickupProvince,PickupPostalCode,DropoffStreet,DropoffSubDistrict,DropoffDistrict,DropoffProvince,DropoffPostalCode,ItemDescription,WeightKg,VolumeCBM,Quantity");
        csv.AppendLine("not-a-guid,Normal,123 St,Sub,Dist,BKK,10000,456 Ave,Sub,Dist,BKK,10000,Box,5.0,0.3,1");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv.ToString()));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", "orders.csv");

        var response = await Client.PostAsync("/api/orders/import", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Tms.Orders.Application.Features.ImportOrder.ImportOrdersResult>();
        result!.FailCount.Should().BeGreaterThan(0);
        result.Errors.Should().NotBeEmpty();
    }
}
