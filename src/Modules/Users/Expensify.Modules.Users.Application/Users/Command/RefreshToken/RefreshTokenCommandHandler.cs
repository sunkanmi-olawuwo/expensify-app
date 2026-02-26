using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Application.Users.Command.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IIdentityProviderService identityProviderService)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        Result<RefreshTokenResponse> result = await identityProviderService.RefreshTokenAsync(
            request.Token, request.RefreshToken,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<RefreshTokenResponse>(result.Error);
        }

        return result;
    }
}
