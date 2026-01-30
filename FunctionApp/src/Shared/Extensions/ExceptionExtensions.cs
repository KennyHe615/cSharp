using System.Reflection;
using System.Text.Json;


namespace Shared.Extensions;

public static class ExceptionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
                                                                {
                                                                    WriteIndented = true
                                                                };

    private static readonly string[] ExceptionPropertyNames = typeof(Exception)
                                                              .GetProperties(
                                                                  BindingFlags.Public | BindingFlags.Instance)
                                                              .Select(p => p.Name)
                                                              .ToArray();

    public static string ToJson(this Exception ex)
    {
        Dictionary<string, object?> payload = ToObject(ex);

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static Dictionary<string, object?> ToObject(Exception ex)
    {
        Dictionary<string, object?> obj = new()
                                          {
                                              ["type"] = ex.GetType().FullName,
                                              ["message"] = ex.Message,
                                              ["source"] = ex.Source,
                                              ["hresult"] = ex.HResult
                                          };

        // Dynamically include any custom properties from the derived exception
        PropertyInfo[] customProps = ex.GetType()
                                       .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => !ExceptionPropertyNames.Contains(p.Name))
                                       .ToArray();

        foreach (PropertyInfo prop in customProps)
        {
            try
            {
                object? value = prop.GetValue(ex);
                obj[char.ToLower(prop.Name[0]) + prop.Name[1..]] = value;
            }
            catch
            {
                // Ignore properties that fail to evaluate
            }
        }

        if (ex is AggregateException agg)
        {
            obj["innerExceptions"] = agg.InnerExceptions.Select(ToObject).ToList();
        }
        else if (ex.InnerException is not null)
        {
            obj["innerException"] = ToObject(ex.InnerException);
        }

        return obj;
    }
}
