using NetArchTest.Rules;

namespace Expensify.Modules.Users.ArchitectureTests.Abstractions;

internal static class TestResultExtensions
{
    internal static void ShouldBeSuccessful(this TestResult testResult)
    {
        Assert.That(testResult.FailingTypes, Is.Null.Or.Empty);
    }

    internal static void ShouldBeNullOrEmpty(this List<Type>? failingTypes)
    {
        Assert.That(failingTypes, Is.Null.Or.Empty);
    }
}
