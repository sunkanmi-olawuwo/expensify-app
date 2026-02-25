using System.Reflection;

namespace Expensify.Modules.Expenses.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
