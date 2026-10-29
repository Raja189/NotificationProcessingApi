using Microsoft.AspNetCore.Mvc;
using Notification.Api.DTOs;
using Notification.Api.Services;

namespace Notification.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            if (result.IsDuplicate)
            {
                Response.Headers["Idempotent-Replay"] = "true";
                return Ok(result.Notification);
            }
            return CreatedAtAction(nameof(GetById), new { id = result.Notification.Id }, result.Notification);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("request", ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var notification = await service.GetByIdAsync(id, cancellationToken);
        return notification is null ? NotFound(new ProblemDetails { Title = "Notification not found", Detail = $"No notification exists with ID '{id}'.", Status = 404 }) : Ok(notification);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(string customerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId)) return BadRequest(new ProblemDetails { Title = "Customer ID is required", Status = 400 });
        return Ok(await service.GetByCustomerAsync(customerId, cancellationToken));
    }

    [HttpPost("process-pending")]
    public async Task<ActionResult<ProcessingSummary>> ProcessPending(CancellationToken cancellationToken) =>
        Ok(await service.ProcessPendingAsync(cancellationToken));
}
