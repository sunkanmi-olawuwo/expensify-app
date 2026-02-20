using NetArchTest.Rules;

namespace Expensify.ArchitectureTests.Abstractions;

internal static class TestResultExtensions
{
    internal static void ShouldBeSuccessful(this TestResult testResult)
    {
        Assert.That(testResult.FailingTypes, Is.Null.Or.Empty);
    }
}
