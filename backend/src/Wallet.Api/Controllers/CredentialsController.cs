using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Contracts;
using Wallet.Tenant;

namespace Wallet.Api.Controllers;

[ApiController]
[Route("api/credentials")]
public sealed class CredentialsController(
    IssueCredentialUseCase issueCredential,
    ListCredentialsUseCase listCredentials) : ControllerBase
{
    /// <summary>UC01: alta de la credencial de un socio.</summary>
    [HttpPost]
    [ProducesResponseType<IssueCredentialResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(IssueCredentialRequest request, CancellationToken ct)
    {
        if (!RequestValidator.TryValidate(request, out var command, out var errors))
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var result = await issueCredential.HandleAsync(command, ct);

        return StatusCode(StatusCodes.Status201Created, IssueCredentialResponse.From(result));
    }

    /// <summary>UC02: listado de las credenciales emitidas, de la más nueva a la más vieja.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CredentialListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(string? dni, string? numeroSocio, CancellationToken ct)
    {
        if (!RequestValidator.TryValidate(dni, numeroSocio, out var errors))
        {
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var summaries = await listCredentials.HandleAsync(new ListCredentialsQuery(dni, numeroSocio), ct);

        return Ok(summaries.Select(CredentialListItem.From).ToList());
    }
}
