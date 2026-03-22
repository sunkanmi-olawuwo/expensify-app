using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Application.Users.Command.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(IIdentityProviderService identityProviderService)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        return await identityProviderService.ForgotPasswordAsync(request.Email, cancellationToken);
    }
}
