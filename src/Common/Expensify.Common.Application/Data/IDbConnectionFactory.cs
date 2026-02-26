using System.Data.Common;

namespace Expensify.Common.Application.Data;

public interface IDbConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync();
}
