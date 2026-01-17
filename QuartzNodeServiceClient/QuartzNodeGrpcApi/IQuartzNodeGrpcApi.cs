using GrpcService;
using QuartzNodeService.Models;

namespace QuartzNodeService.QuartzNodeGrpcApi
{
    public interface IQuartzNodeGrpcApi
    {
        public int Attempt { get; set; }
        public Task ConnectToControllerStream(CancellationToken cts);

    }
}

