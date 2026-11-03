using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Services;
using OrderManagement.Infrastructure.Inventory;
using OrderManagement.Infrastructure.Notifications;
using OrderManagement.Infrastructure.Observers;
using OrderManagement.Infrastructure.Payments;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<InMemoryProductRepository>(_ => new InMemoryProductRepository());
builder.Services.AddSingleton<IProductRepository>(serviceProvider =>
    serviceProvider.GetRequiredService<InMemoryProductRepository>());
builder.Services.AddSingleton<InMemoryOrderRepository>();
builder.Services.AddSingleton<IOrderRepository>(serviceProvider =>
    serviceProvider.GetRequiredService<InMemoryOrderRepository>());
builder.Services.AddSingleton<InMemoryInventoryRepository>();
builder.Services.AddSingleton<IInventoryRepository>(serviceProvider =>
    serviceProvider.GetRequiredService<InMemoryInventoryRepository>());

builder.Services.AddSingleton<InMemoryInventoryObserver>();
builder.Services.AddSingleton<IInventoryObserver>(serviceProvider =>
    serviceProvider.GetRequiredService<InMemoryInventoryObserver>());
builder.Services.AddSingleton<IInventorySubject>(serviceProvider =>
{
    var inventory = serviceProvider.GetRequiredService<InMemoryInventoryRepository>();
    inventory.Subscribe(serviceProvider.GetRequiredService<InMemoryInventoryObserver>());
    return inventory;
});

builder.Services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
builder.Services.AddSingleton<INotificationService, FakeNotificationService>();
builder.Services.AddTransient<CreateOrderUseCase>();
builder.Services.AddTransient<GetOrderUseCase>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
