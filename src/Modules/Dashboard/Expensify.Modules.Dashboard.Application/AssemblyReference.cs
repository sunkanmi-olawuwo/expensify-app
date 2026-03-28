using System.Reflection;

namespace Expensify.Modules.Dashboard.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
