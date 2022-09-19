using System;
using System.Linq;
using System.Threading.Tasks;
using Housekeeper.Database;
using Housekeeper.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qmmands.Default;

namespace Housekeeper.Commands.TypeParsers;

public class JobTypeParser : TypeParser<Job>
{
    public override async ValueTask<ITypeParserResult<Job>> ParseAsync(ICommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var db = context.Services.GetRequiredService<DatabaseContext>();

        if (int.TryParse(value.Span, out var jobId))
        {
            var job = await db.Jobs.FindAsync(jobId);
            if (job is not null) return Success(job);
        }

        var jobName = value.ToString();
        var matches = await db.Jobs.Where(x => EF.Functions.ILike(x.Name, jobName)).ToListAsync();

        return matches.Count switch
        {
            0 => Failure("No job was found matching the input."),
            1 => Success(matches[0]),
            _ => Failure("Multiple jobs were found matching the input. Specify the job ID instead.")
        };
    }
}
