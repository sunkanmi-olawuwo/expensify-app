using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Application.Users.Command.ResetPassword;

internal sealed class ResetPasswordCommandHandler(IIdentityProviderService identityProviderService)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        return await identityProviderService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);
    }
}
