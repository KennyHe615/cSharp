using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using SharedKernel.Extensions;


namespace Infrastructure.Persistence.Converters;

internal sealed class NullableEnumToSnakeUpperStringConverter<TEnum>()
    : ValueConverter<TEnum?, string?>(v => v.HasValue ? v.Value.WriteEnumSnakeUpper() : null,
                                      v => string.IsNullOrWhiteSpace(v) ? null : v.ReadEnum<TEnum>())
    where TEnum : struct, Enum;
