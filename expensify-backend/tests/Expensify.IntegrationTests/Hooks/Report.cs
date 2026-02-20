#region

using System.Text;
using System.Text.Json.Nodes;
using Allure.Net.Commons;
using Reqnroll;

#endregion

namespace Expensify.IntegrationTests.Hooks;

[Binding]
internal static class Reports
{
    private static readonly string AssemblyName = typeof(Reports).Assembly.GetName().Name ?? "UnknownAssembly";
    private static readonly string[] LabelToRemove = ["feature", "package", "host", "suite"];


    [BeforeScenario(Order = 100)]
    public static void BeforeScenario(FeatureContext featureContext)
    {
        AllureLifecycle.Instance.UpdateTestCase(tr =>
        {
            string? packageLabel = tr.labels.FirstOrDefault(l => l.name == "package")?.value;
            packageLabel ??= featureContext.FeatureInfo.FolderPath.Replace('/', '.').Replace('\\', '.');
            tr.labels.RemoveAll(l => LabelToRemove.Contains(l.name));
            tr.labels.Add(Label.Package($"{AssemblyName}.{packageLabel}"));


            tr.labels.Add(Label.Suite("Expensify API Tests"));
            tr.labels.Add(Label.SubSuite(featureContext.FeatureInfo.Title));

            if (Environment.GetEnvironmentVariable("TEST_BATCH") is not null)
            {
                var config = JsonNode.Parse(Environment.GetEnvironmentVariable("TEST_BATCH")!);
                if (config is null)
                {
                    throw new InvalidOperationException(
                        "TEST_BATCH environment variable is not a valid JSON object.");
                }

                string? repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
                repo ??= "UnknownRepository";
                string host = $"{repo} - {config["name"]} - Batch {config["batch"]}";
                tr.labels.Add(Label.Host(host));
            }
            else
            {
                tr.labels.Add(Label.Host("Local Developer Machine"));
            }
        });
    }


    public static class Attachments
    {
        public static void AddText(string title, string body, bool attachToStep = true)
        {
            AllureApi.AddAttachment(title, "text/plain", Encoding.UTF8.GetBytes(body));
            if (!attachToStep)
            {
                AllureLifecycle.Instance.UpdateExecutableItem(ex =>
                {
                    AllureLifecycle.Instance.UpdateTestCase(tc =>
                    {
                        Attachment a = ex.attachments.First(a => a.name.Equals(title, StringComparison.Ordinal));
                        tc.attachments.Add(a);
                        ex.attachments.Remove(a);
                    });
                });
            }
        }
    }
}
