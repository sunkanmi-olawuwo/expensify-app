using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Preferences;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Users.Command.UpdateUser;

internal sealed class UpdateUserCommandHandler(
    IUserRepository userRepository,
    IUserPreferenceCatalogService userPreferenceCatalogService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        Result validationResult = await userPreferenceCatalogService.ValidateSelectionsAsync(
            request.Currency,
            request.Timezone,
            user.Currency,
            user.Timezone,
            cancellationToken);

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        user.Update(
            request.FirstName,
            request.LastName,
            request.Currency,
            request.Timezone,
            request.MonthStartDay);

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
