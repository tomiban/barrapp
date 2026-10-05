using Barrapp.Application.Abstractions;
using Barrapp.Domain.Common;

namespace Barrapp.Application.Features.Ping;

internal sealed class PingQueryHandler : IQueryHandler<PingQuery, PingResponse>
{
    public Task<Result<PingResponse>> Handle(PingQuery request, CancellationToken cancellationToken)
    {
        var response = new PingResponse("pong", DateTimeOffset.UtcNow);
        return Task.FromResult(Result.Success(response));
    }
}
