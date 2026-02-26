using MediatR;
using Expensify.Common.Domain;

namespace Expensify.Common.Application.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
