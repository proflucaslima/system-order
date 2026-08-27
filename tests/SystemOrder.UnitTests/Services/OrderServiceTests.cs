using Moq;
using SystemOrder.Application.DTOs;
using SystemOrder.Application.Interfaces;
using SystemOrder.Application.Services;
using SystemOrder.Domain.Entities;

namespace SystemOrder.UnitTests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repositoryMock;
    private readonly Mock<IOrderCache> _cacheMock;

    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _repositoryMock =
            new Mock<IOrderRepository>();

        _cacheMock =
            new Mock<IOrderCache>();

        _service =
            new OrderService(
                _repositoryMock.Object,
                _cacheMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateOrder()
    {
        // Arrange
        var request = new CreateOrderRequest(
            "Lucas",
            "Notebook",
            2,
            5000);

        // Act
        var result =
            await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Lucas", result.CustomerName);
        Assert.Equal("Notebook", result.Product);
        Assert.Equal(2, result.Quantity);
        Assert.Equal(10000, result.Total);

        _repositoryMock.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<Order>()),
            Times.Once);

        _cacheMock.Verify(
            cache => cache.Remove(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheExists_ShouldNotCallRepository()
    {
        // Arrange
        var orders = new List<Order>
        {
            new(
                "Lucas",
                "Notebook",
                1,
                5000)
        };

        _cacheMock
            .Setup(cache => cache.Get())
            .Returns(orders);

        // Act
        var result =
            await _service.GetAllAsync();

        // Assert
        Assert.Single(result);

        _repositoryMock.Verify(
            repository =>
                repository.GetAllAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheDoesNotExist_ShouldCallRepository()
    {
        // Arrange
        var orders = new List<Order>
        {
            new(
                "Lucas",
                "Notebook",
                1,
                5000)
        };

        _cacheMock
            .Setup(cache => cache.Get())
            .Returns(
                (IEnumerable<Order>?)null);

        _repositoryMock
            .Setup(repository =>
                repository.GetAllAsync())
            .ReturnsAsync(orders);

        // Act
        var result =
            await _service.GetAllAsync();

        // Assert
        Assert.Single(result);

        _repositoryMock.Verify(
            repository =>
                repository.GetAllAsync(),
            Times.Once);

        _cacheMock.Verify(
            cache =>
                cache.Set(
                    It.IsAny<IEnumerable<Order>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenOrderExists_ShouldUpdateOrder()
    {
        // Arrange
        var order = new Order(
            "Lucas",
            "Notebook",
            1,
            5000);

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        var request =
            new UpdateOrderRequest(
                "Lucas Lima",
                "MacBook",
                2,
                8000);

        // Act
        var result =
            await _service.UpdateAsync(
                order.Id,
                request);

        // Assert
        Assert.True(result);

        Assert.Equal(
            "Lucas Lima",
            order.CustomerName);

        Assert.Equal(
            "MacBook",
            order.Product);

        _repositoryMock.Verify(
            repository =>
                repository.UpdateAsync(order),
            Times.Once);

        _cacheMock.Verify(
            cache => cache.Remove(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenOrderExists_ShouldDeleteOrder()
    {
        // Arrange
        var order = new Order(
            "Lucas",
            "Notebook",
            1,
            5000);

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        // Act
        var result =
            await _service.DeleteAsync(order.Id);

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(
            repository =>
                repository.DeleteAsync(order.Id),
            Times.Once);

        _cacheMock.Verify(
            cache => cache.Remove(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenOrderDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var id = Guid.NewGuid();

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(id))
            .ReturnsAsync(
                (Order?)null);

        // Act
        var result =
            await _service.DeleteAsync(id);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(
            repository =>
                repository.DeleteAsync(
                    It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenQuantityIsZero_ShouldThrowException()
    {
        // Arrange
        var request =
            new CreateOrderRequest(
                "Lucas",
                "Notebook",
                0,
                5000);

        // Act
        var action = async () =>
            await _service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(
            action);
    }
}