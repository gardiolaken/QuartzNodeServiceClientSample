using Grpc.Core;
using GrpcService;

namespace QuartzNodeService.QuartzSchedulerService
{
    public interface IQuartzSchedulerEngine
    {
        Task<QuartzClientResponse_Stream_PROTO> DeleteJob(QuartzClientRequest_Stream_PROTO request);

        Task<QuartzClientResponse_Stream_PROTO> GetJob(QuartzClientRequest_Stream_PROTO request);

        Task<QuartzClientResponse_Stream_PROTO> CreateJob(QuartzClientRequest_Stream_PROTO request);

        Task<QuartzClientResponse_Stream_PROTO> UpdateJob(QuartzClientRequest_Stream_PROTO request);

        Task<QuartzClientResponse_Stream_PROTO> CreateRunNowJob(QuartzClientRequest_Stream_PROTO request);

        //Task StartUpProcess();
    }
}
