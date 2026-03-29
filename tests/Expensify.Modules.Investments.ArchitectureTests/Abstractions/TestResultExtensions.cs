namespace Expensify.Modules.Investments.ArchitectureTests.Abstractions;

internal static class TestResultExtensions
{
    internal static void ShouldBeSuccessful(this NetArchTest.Rules.TestResult result)
    {
        Assert.That(result.IsSuccessful, Is.True, string.Join(Environment.NewLine, result.FailingTypeNames ?? []));
    }

    internal static void ShouldBeNullOrEmpty<T>(this IEnumerable<T> source)
    {
        Assert.That(source, Is.Empty);
    }
}
