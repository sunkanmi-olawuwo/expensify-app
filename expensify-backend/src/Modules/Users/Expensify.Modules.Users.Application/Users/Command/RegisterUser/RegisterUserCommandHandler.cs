using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Users.Command.RegisterUser;

internal sealed class RegisterUserCommandHandler(
    IIdentityProviderService identityProviderService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        Result<string> result = await identityProviderService.RegisterUserAsync(
            new RegisterUserRequest(request.Email, request.Password, request.FirstName, request.LastName, request.Role),
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(result.Error);
        }

        var user = User.Create(request.FirstName, request.LastName, result.Value);

        userRepository.Add(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResponse(user.Id);
    }
}
