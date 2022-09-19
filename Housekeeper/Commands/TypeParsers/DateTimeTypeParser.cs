using System;
using System.Globalization;
using System.Threading.Tasks;
using Qmmands;
using Qmmands.Default;

namespace Housekeeper.Commands.TypeParsers;

public class DateTimeTypeParser : TypeParser<DateTime>
{
    private static CultureInfo _culture = new("en-GB");

    public override ValueTask<ITypeParserResult<DateTime>> ParseAsync(ICommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        if (DateTime.TryParseExact(
                value.ToString(),
                "dd/MM/yyyy",
                _culture,
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            return Success(date.ToUniversalTime());
        }

        return Failure("Failed to parse a DateTime. Provide input in the format dd/MM/yyyy.");
    }
}
