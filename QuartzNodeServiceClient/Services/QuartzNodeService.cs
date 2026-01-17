//using Google.Protobuf.Collections;
//using Grpc.Core;
//using Quartz;
//using QuartzNodeService.QuartzSchedulerService;
//using System.Text;

//namespace GrpcService.Services
//{
//	public class QuartzNodeService : QuartzNode.QuartzNodeBase
//	{
//		private readonly ILogger<QuartzNodeService> _logger;
//		private QuartzSchedulerEngine _quartzScheduler;
//		public QuartzNodeService(ILogger<QuartzNodeService> logger, ISchedulerFactory schedulerFactory, QuartzSchedulerEngine quartzSchedulerEngine)
//		{
//			_logger = logger;
//			_quartzScheduler = quartzSchedulerEngine;
//		}

//		public override Task<HelloReply_PROTO> SayHello(HelloRequest_PROTO request, ServerCallContext context)
//		{
//			return Task.FromResult(new HelloReply_PROTO
//			{
//				Message = "Hello " + request.Name
//			});
//		}

//		public override async Task<QuartzJobResponse_PROTO> GetJob(QuartzJobRequest_PROTO request, ServerCallContext context)
//		{
//			var response = new QuartzJobResponse_PROTO();
//			try
//			{
//				return await _quartzScheduler.GetJobAsync(request, context);
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "Error in GetJob");
//				throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
//			}
//		}

//		public override async Task<QuartzCRUDResponse_PROTO> CreateJob(QuartzJobRequest_PROTO request, ServerCallContext context)
//		{
//			try
//			{
//				return await _quartzScheduler.CreateJob(request, context);
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "Error in CreateJob");
//				throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
//			}
//		}

//		public override async Task<QuartzCRUDResponse_PROTO> CreateRunNowJob(QuartzJobRequest_PROTO request, ServerCallContext context)
//		{
//			try
//			{
//				return await _quartzScheduler.CreateRunNowJob(request, context);
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "Error in CreateRunNowJob");
//				throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
//			}
//		}

//		public override async Task<QuartzCRUDResponse_PROTO> DeleteJob(QuartzJobRequest_PROTO request, ServerCallContext context)
//		{
//			try
//			{
//				return await _quartzScheduler.DeleteJob(request, context);
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "Error in DeleteJob");
//				throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
//			}
//		}

//		public override async Task<QuartzCRUDResponse_PROTO> UpdateJob(QuartzJobRequest_PROTO request, ServerCallContext context)
//		{
//			try
//			{
//				return await _quartzScheduler.UpdateJob(request, context);
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "Error in UpdateJob");
//				throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
//			}
//		}

//    }
//}
