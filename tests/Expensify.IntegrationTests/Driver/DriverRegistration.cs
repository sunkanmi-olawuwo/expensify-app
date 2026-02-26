using Reqnroll;
using Reqnroll.BoDi;

namespace Expensify.IntegrationTests.Driver;

[Binding]
public static class DriverRegistration
{
    private static ApiDriver? _driver;

    [BeforeTestRun]
    public static async Task BeforeTestRun(IObjectContainer diContainer)
    {
        _driver = await ApiDriver.CreateAsync();

        diContainer.RegisterInstanceAs(_driver);
        diContainer.RegisterInstanceAs(_driver.HttpClient);
        diContainer.RegisterInstanceAs(_driver.ExpensifyV1Client);
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_driver is not null)
        {
            await _driver.DisposeAsync();
            _driver = null;
        }
    }
}
