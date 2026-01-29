using System.Text.Json;


namespace Shared.Extensions;

public static class ExceptionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
                                                                {
                                                                    WriteIndented = true
                                                                };

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
