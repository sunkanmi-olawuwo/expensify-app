using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Application.Users.Command.ChangePassword;

internal sealed class ChangePasswordCommandHandler(IIdentityProviderService identityProviderService)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        return await identityProviderService.ChangePasswordAsync(
            request.UserId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);
    }
}
