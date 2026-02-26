using System.Reflection;

namespace Expensify.Modules.Expenses.Presentation;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
