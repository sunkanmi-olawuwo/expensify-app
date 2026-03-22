using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;

namespace Expensify.Modules.Users.Application.Users.Command.Logout;

internal sealed class LogoutCommandHandler(IIdentityProviderService identityProviderService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        return await identityProviderService.LogoutAsync(request.UserId, cancellationToken);
    }
}
