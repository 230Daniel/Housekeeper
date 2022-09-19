using System;
using System.Linq;
using System.Threading.Tasks;
using Housekeeper.Entities;
using Housekeeper.Services;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Qmmands.Default;

namespace Housekeeper.Commands.TypeParsers;

public class JobTypeParser : TypeParser<Job>
{
    public override async ValueTask<ITypeParserResult<Job>> ParseAsync(ICommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var jobService = context.Services.GetRequiredService<JobService>();

        if (int.TryParse(value.Span, out var jobId))
        {
            var job = await jobService.GetByIdAsync(jobId);
            if (job is not null) return Success(job);
        }

        var jobName = value.ToString();
        var matches = (await jobService.GetByNameAsync(jobName)).ToArray();

        return matches.Length switch
        {
            0 => Failure("No job was found matching the input."),
            1 => Success(matches[0]),
            _ => Failure("Multiple jobs were found matching the input. Specify the job ID instead.")
        };
    }
}
