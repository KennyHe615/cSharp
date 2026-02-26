using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using SharedKernel.Extensions;


namespace Infrastructure.Persistence.Converters;

internal sealed class EnumToSnakeUpperStringConverter<TEnum>()
    : ValueConverter<TEnum, string>(v => v.WriteEnumSnakeUpper(), v => v.ReadEnum<TEnum>())
    where TEnum : struct, Enum;
