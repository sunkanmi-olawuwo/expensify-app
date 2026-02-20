using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Expensify.Api.Client;
using Expensify.IntegrationTests.Hooks;
using Serilog;
using System.Net;
using ExpensifyV1Client = Expensify.Api.Client.ExpensifyV1Client;

namespace Expensify.IntegrationTests.Driver;

public sealed class ApiDriver : IAsyncDisposable
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:latest")
        .WithDatabase("Expensify")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:7.0").Build();

    private WebApplicationFactory<Program>? _webApp;

    private ApiDriver() { }

    public static async Task<ApiDriver> CreateAsync()
    {
        var driver = new ApiDriver();
        await driver.InitializeAsync();
        return driver;
    }

    private async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings:Database", _dbContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings:Cache", _redisContainer.GetConnectionString());

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(ApiProjectFolder.FullName)
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables()
            .Build();

        _webApp = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder
                .UseEnvironment("Development")
                .UseContentRoot(ApiProjectFolder.FullName)
                .ConfigureServices(services =>
                {
                    services.AddSerilog(l =>
                    {
                        LoggerConfiguration c = l.ReadFrom.Configuration(configuration);
                        c.WriteTo.TestSink();
                    });
                })
                .ConfigureTestServices(services =>
                {
                    services.ConfigureHttpClientDefaults(serviceBuilder =>
                    {
                        serviceBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                        {
                            CookieContainer = TestCookieContainer,
                            UseCookies = true
                        });
                    });
                }));

        HttpClient = _webApp.CreateClient();
        ExpensifyV1Client = new ExpensifyV1Client(_webApp.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(_webApp.ClientOptions.BaseAddress, "api/v1/")
        }));
    }

    public HttpClient HttpClient { get; private set; } = default!;
    public IExpensifyV1Client ExpensifyV1Client { get; private set; } = default!;
    public TestServer Server => _webApp!.Server;
    public CookieContainer TestCookieContainer { get; } = new();

    private static DirectoryInfo TestBinFolder { get; } =
       new DirectoryInfo(typeof(ApiDriver).Assembly.Location).Parent!;

    private static DirectoryInfo RootFolder { get; } = TestBinFolder.Parent?.Parent?.Parent?.Parent?.Parent!;
    private static DirectoryInfo ApiProjectFolder { get; } =
        new(Path.Combine(RootFolder.FullName, "src", "API", "Expensify.Api"));

    public static DirectoryInfo CaptureFolder { get; } = RootFolder.CreateSubdirectory(".capture");


    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        if (_webApp is not null)
        {
            await _webApp.DisposeAsync();
            _webApp = null;
        }

        await _redisContainer.StopAsync();
        await _dbContainer.StopAsync();
    }
}
