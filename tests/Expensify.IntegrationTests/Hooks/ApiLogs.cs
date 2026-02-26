using System.Collections;
using Reqnroll;
using Expensify.IntegrationTests.Driver;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Expensify.IntegrationTests.Hooks;

[Binding]
public static class ApiLogs
{
    private sealed class Sink(IFormatProvider? formatProvider) : ILogEventSink
    {
        private readonly MessageTemplateTextFormatter _textFormatter = new(
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            formatProvider);

        public void Emit(LogEvent logEvent)
        {
            using StringWriter stringWriter = new();
            _textFormatter.Format(logEvent, stringWriter);
            string message = stringWriter.ToString();
            LogEvents.Add((logEvent, message));
        }

        public List<(LogEvent Event, string FormattedMessage)> LogEvents { get; } = new();
    }

    public static IReadOnlyList<(LogEvent Event, string FormattedMessage)> Logs => _apiLogSink?.LogEvents ?? [];

    private static Sink? _apiLogSink;

    public static LoggerConfiguration TestSink(this LoggerSinkConfiguration loggerConfiguration,
        IFormatProvider? formatProvider = null)
    {
        _apiLogSink ??= new Sink(formatProvider);
        return loggerConfiguration.Sink(_apiLogSink);
    }

    private static DirectoryInfo? _apiLogsFolder;

    [BeforeTestRun(Order = 1001)]
    public static void BeforeTestRun()
    {
        if (ApiDriver.CaptureFolder.Exists)
        {
            ApiDriver.CaptureFolder.Delete(true);
        }

        ApiDriver.CaptureFolder.Create();

        _apiLogsFolder = ApiDriver.CaptureFolder.CreateSubdirectory("apiLogs");
        if (_apiLogsFolder.Exists)
        {
            _apiLogsFolder.Delete(true);
        }

        _apiLogsFolder.Create();
    }

    [BeforeScenario(Order = 1001)]
    public static void BeforeScenario(ScenarioContext scenarioContext, FeatureContext featureContext)
    {
        _apiLogSink?.LogEvents.Clear();
        string arguments = scenarioContext.ScenarioInfo.Arguments.Cast<DictionaryEntry>()
            .Aggregate("", (current, i) => $"{current}_{i.Value}").Replace("\"", "");
        string name =
            $"{featureContext.FeatureInfo.Title}_{scenarioContext.ScenarioInfo.Title}_{arguments}.log"
                .Replace(" ", "_");
        if (_apiLogsFolder == null)
        {
            throw new Exception("Missing console logs folder");
        }

        string filePath = Path.Join(_apiLogsFolder.FullName, name);
        scenarioContext.Set(filePath, "ApiLogsFile");
    }


    [AfterScenario(Order = 1001)]
    public static void AfterScenario(ScenarioContext scenarioContext)
    {
        try
        {
            string filePath = scenarioContext.Get<string>("ApiLogsFile");
            bool testFailed = scenarioContext.TestError != null;

            if (_apiLogSink == null || _apiLogSink.LogEvents.Count == 0)
            {
                if (testFailed)
                {
                    Console.Out.WriteLine("=== TEST FAILED ===");
                    Console.Out.WriteLine($"Scenario: {scenarioContext.ScenarioInfo.Title}");
                    Console.Out.WriteLine($"Error: {scenarioContext.TestError?.Message}");
                    Console.Out.WriteLine($"Stack Trace: {scenarioContext.TestError?.StackTrace}");
                    Console.Out.WriteLine("No API logs captured.");
                    Console.Out.WriteLine("===================");
                }
                else
                {
                    Console.Out.WriteLine("No API logs to write.");
                }
                return;
            }

            string text = string.Join("", _apiLogSink.LogEvents.Select(l => l.FormattedMessage));
            File.WriteAllText(filePath, text);
            Reports.Attachments.AddText("Api Log", text, false);

            // Output logs to console when test fails for CI visibility
            if (testFailed)
            {
                Console.Out.WriteLine("=== TEST FAILED ===");
                Console.Out.WriteLine($"Scenario: {scenarioContext.ScenarioInfo.Title}");
                Console.Out.WriteLine($"Error: {scenarioContext.TestError?.Message}");
                Console.Out.WriteLine($"Stack Trace: {scenarioContext.TestError?.StackTrace}");
                Console.Out.WriteLine("=== API LOGS ===");
                Console.Out.WriteLine(text);
                Console.Out.WriteLine("===================");
            }

            _apiLogSink.LogEvents.Clear();
        }
        catch (IOException ioEx)
        {
            Console.Out.WriteLine(ioEx.Message);
        }
        catch (UnauthorizedAccessException uaEx)
        {
            Console.Out.WriteLine(uaEx.Message);
        }
    }
}
