using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Application.Users.Command.Login;

internal sealed class LoginCommandHandler(
    IIdentityProviderService identityProviderService)
    : ICommandHandler<LoginCommand, LoginUserResponse>
{
    public async Task<Result<LoginUserResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        Result<LoginUserResponse> result = await identityProviderService.LoginAsync(
            request.Email, request.Password,
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<LoginUserResponse>(result.Error);
        }

        return Result.Success(result.Value);
    }
}
