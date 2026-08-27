using Microsoft.AspNetCore.Mvc;
using SystemOrder.Application.DTOs;
using SystemOrder.Application.Interfaces;

namespace SystemOrder.Api.Controllers;

/// <summary>
/// Endpoints responsáveis pelo gerenciamento de pedidos.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Retorna todos os pedidos cadastrados.
    /// </summary>
    /// <remarks>
    /// A consulta utiliza cache para melhorar a performance.
    ///
    /// Se os pedidos estiverem disponíveis em cache,
    /// o repository não será consultado.
    /// </remarks>
    /// <response code="200">Pedidos retornados com sucesso.</response>
    /// <response code="401">API Key não informada ou inválida.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Cria um novo pedido.
    /// </summary>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     POST /api/orders
    ///
    ///     {
    ///         "customerName": "Lucas",
    ///         "product": "Notebook",
    ///         "quantity": 2,
    ///         "unitPrice": 5000
    ///     }
    ///
    /// Após a criação do pedido, o cache de pedidos é invalidado.
    /// </remarks>
    /// <param name="request">Dados necessários para criação do pedido.</param>
    /// <response code="201">Pedido criado com sucesso.</response>
    /// <response code="400">Dados do pedido são inválidos.</response>
    /// <response code="401">API Key não informada ou inválida.</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request)
    {
        try
        {
            var order = await _orderService.CreateAsync(request);

            return Created(
                $"/api/orders/{order.Id}",
                order);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    /// <summary>
    /// Atualiza um pedido existente.
    /// </summary>
    /// <remarks>
    /// Atualiza os dados de um pedido através do seu identificador.
    ///
    /// Após a atualização, o cache de pedidos é invalidado.
    ///
    /// Exemplo:
    ///
    ///     PUT /api/orders/{id}
    ///
    ///     {
    ///         "customerName": "Lucas Lima",
    ///         "product": "MacBook",
    ///         "quantity": 2,
    ///         "unitPrice": 8000
    ///     }
    ///
    /// </remarks>
    /// <param name="id">Identificador único do pedido.</param>
    /// <param name="request">Novos dados do pedido.</param>
    /// <response code="204">Pedido atualizado com sucesso.</response>
    /// <response code="400">Dados informados são inválidos.</response>
    /// <response code="401">API Key não informada ou inválida.</response>
    /// <response code="404">Pedido não encontrado.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var updated = await _orderService.UpdateAsync(
                id,
                request);

            if (!updated)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    /// <summary>
    /// Remove um pedido existente.
    /// </summary>
    /// <remarks>
    /// Remove um pedido através do seu identificador.
    ///
    /// Após a exclusão, o cache de pedidos é invalidado.
    /// </remarks>
    /// <param name="id">Identificador único do pedido.</param>
    /// <response code="204">Pedido removido com sucesso.</response>
    /// <response code="401">API Key não informada ou inválida.</response>
    /// <response code="404">Pedido não encontrado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _orderService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        return NoContent();
    }
}