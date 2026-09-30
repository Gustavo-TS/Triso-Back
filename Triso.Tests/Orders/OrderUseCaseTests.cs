using Triso.Application.Orders;
using Triso.Application.Ports.Persistence;
using Triso.Domain.Entities;
using Triso.Domain.Enums;

namespace Triso.Tests.Orders;
public sealed class OrderUseCaseTests
{
    [Fact]
    public async Task Create_calculates_total_from_persisted_products()
    {
        var product = new Product { Name = "Produto", Slug = "produto", PriceCents = 1250, Status = "published", CategoryId = Guid.NewGuid() };
        var orderRepo = new Orders(); var payments = new Payments();
        var useCase = new CreateOrderUseCase(new Products(product), orderRepo, payments, new UnitOfWork());
        await useCase.ExecuteAsync(Guid.NewGuid(), Request(product.Id, 3), CancellationToken.None);
        var order = Assert.Single(orderRepo.Values);
        Assert.Equal(3750, order.TotalCents);
        Assert.Equal(1250, Assert.Single(order.Items).UnitPriceCents);
        Assert.Equal(3750, Assert.Single(order.Items).TotalCents);
    }

    [Fact]
    public async Task Create_rejects_non_published_product()
    {
        var product = new Product { Name = "Rascunho", Slug = "rascunho", PriceCents = 100, Status = "draft", CategoryId = Guid.NewGuid() };
        var useCase = new CreateOrderUseCase(new Products(product), new Orders(), new Payments(), new UnitOfWork());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => useCase.ExecuteAsync(Guid.NewGuid(), Request(product.Id, 1), CancellationToken.None));
    }

    [Fact]
    public void Order_rejects_invalid_status_transition()
    {
        var order = new Order { OrderNumber = "TRI-TEST", Address = Address() };
        Assert.Throws<InvalidOperationException>(() => order.ChangeStatus(OrderStatus.Shipped));
    }

    private static CreateOrderRequest Request(Guid productId, int quantity) => new([new OrderProductRequest(productId, quantity)], new OrderAddressRequest("Cliente", "Rua", "1", null, "Centro", "S\u00e3o Paulo", "SP", "01000-000"));
    private static OrderAddress Address() => new() { RecipientName = "Cliente", Street = "Rua", Number = "1", Neighborhood = "Centro", City = "S\u00e3o Paulo", State = "SP", PostalCode = "01000-000" };
    private sealed class Products(params Product[] products) : IProductRepository { public Task<IReadOnlyList<Product>> GetActiveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) => Task.FromResult<IReadOnlyList<Product>>(products.Where(x => ids.Contains(x.Id) && x.Status == "published").ToList()); }
    private sealed class Orders : IOrderRepository
    {
        public List<Order> Values { get; } = [];
        public Task AddAsync(Order order, CancellationToken ct) { Values.Add(order); return Task.CompletedTask; }
        public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Order?>(Values.SingleOrDefault(x => x.Id == id));
        public Task<Order?> GetByOrderNumberAsync(string number, CancellationToken ct) => Task.FromResult<Order?>(Values.SingleOrDefault(x => x.OrderNumber == number));
        public Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Order>>(Values.Where(x => x.UserId == userId).ToList());
    }
    private sealed class Payments : IPaymentRepository
    {
        public Task AddAsync(Payment payment, CancellationToken ct) => Task.CompletedTask;
        public Task<Payment?> GetByOrderIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Payment?>(null);
        public Task<Payment?> GetByOrderNsuAsync(string nsu, CancellationToken ct) => Task.FromResult<Payment?>(null);
        public Task<bool> TransactionExistsAsync(string nsu, CancellationToken ct) => Task.FromResult(false);
    }
    private sealed class UnitOfWork : IUnitOfWork { public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct) => operation(ct); public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1); }
}
