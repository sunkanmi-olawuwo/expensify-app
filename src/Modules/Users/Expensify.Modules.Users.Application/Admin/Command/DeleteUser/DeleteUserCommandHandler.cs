using Microsoft.Extensions.Logging;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Admin.Command.DeleteUser;

internal sealed class DeleteUserCommandHandler(
    IIdentityProviderService identityProviderService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteUserCommandHandler> logger
    )
    : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        User? domainUser = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        if (domainUser is null)
        {
            logger.LogWarning("Domain user with ID '{UserId}' not found", request.Id);
            return Result.Failure(UserErrors.NotFound(request.Id));
        }

        Result result =await identityProviderService.DeleteUserAsync(domainUser.IdentityId, cancellationToken);
        if (result.IsFailure)
        {
            logger.LogError("Failed to delete user with ID '{UserId}' from identity provider: {Error}", request.Id, result.Error);
            return Result.Failure(result.Error);
        }

        userRepository.Remove(domainUser);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
