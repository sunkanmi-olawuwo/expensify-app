using System.Reflection;
using NJsonSchema.Generation;

namespace Expensify.Common.Application.OpenApi;

public class CustomSchemaNameGenerator : DefaultSchemaNameGenerator
{
    private const string Suffix = "Model";

    public override string Generate(Type type)
    {
        string schemaName = base.Generate(type);

        if (schemaName.EndsWith(Suffix, StringComparison.Ordinal) && type.GetCustomAttribute<RemoveModelSuffixAttribute>() != null)
        {
            return schemaName[..^Suffix.Length];
        }

        return schemaName;
    }
}
