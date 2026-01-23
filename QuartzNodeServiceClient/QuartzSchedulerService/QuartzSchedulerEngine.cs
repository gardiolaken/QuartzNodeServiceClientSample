using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcService;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Quartz;
using Quartz.Impl.Matchers;
using QuartzNodeService.Logger;
using QuartzNodeService.Models;
using QuartzNodeService.QuartzNodeGrpcApi;
using System.Reflection;
using static Quartz.Logging.OperationName;

namespace QuartzNodeService.QuartzSchedulerService
{
	public class QuartzSchedulerEngine : IQuartzSchedulerEngine
    {
		private IScheduler _scheduler;
		private ILogger _logger;
		private readonly ApiKeyProvider _apiKeyProvider;
        public QuartzSchedulerEngine(ILogger<QuartzSchedulerEngine> logger, ISchedulerFactory scheduler, ApiKeyProvider apiKeyProvider)
		{
			_logger = logger;
			_scheduler = scheduler.GetScheduler().Result;
            _apiKeyProvider = apiKeyProvider;
        }
		public void RegisterScheduler(IScheduler scheduler)
		{
			_scheduler = scheduler;            
        }

		public async Task<QuartzClientResponse_Stream_PROTO> DeleteJob(QuartzClientRequest_Stream_PROTO request)
		{
			var response = new QuartzClientResponse_Stream_PROTO { IsError = false };

			try
			{
                var jobKey = GenerateJobKey(request.Job.JobKey);

                // already deleted or does not exist
                if (!await _scheduler.CheckExists(jobKey))
                    return response;

                var result = await _scheduler.DeleteJob(jobKey);
                if (!result)
                {
                    response.IsError = true;
                    response.ErrorMessage = $"Failed to delete job with key {request.Job.JobKey}.";
                }
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.ErrorMessage = ex.Message;
                _logger.LogError($"Error deleting job:{request.Job.JobName}. ErrorMessage:{ex.Message}");
            }
            
			return response;
		}

		public async Task<QuartzClientResponse_Stream_PROTO> GetJob(QuartzClientRequest_Stream_PROTO request)
		{
			var response = new QuartzClientResponse_Stream_PROTO();
			response.IsError = false;

			try
			{
				var jobKey = GenerateJobKey(request.Job.JobKey);
                var jobDetail = await _scheduler.GetJobDetail(jobKey);
                if (jobDetail == null)
                {
                    response.Job = null;
                    return response;
                }

                var triggers = await _scheduler.GetTriggersOfJob(jobKey);
                var trigger = triggers.FirstOrDefault();
                var cron = string.Empty;
                if (trigger is ICronTrigger cronTrigger)
                    cron = cronTrigger.CronExpressionString;

				response.Job = new QuartzJob_PROTO
				{
					JobKey = jobDetail?.Key.Name,
					Schedule = cron,
					LastRunDate = trigger?.GetPreviousFireTimeUtc()?.ToTimestamp(),
					ServiceInfo = new ServiceInfo
					{
						Server = _apiKeyProvider.ServerName,
						Id = _apiKeyProvider.ServiceID,
						Name = _apiKeyProvider.ServiceName
					}
				};
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.ErrorMessage = ex.Message;
                _logger.LogError($"Error getting job:{request.Job.JobName}. ErrorMessage:{ex.Message}");
            }
            
			return response;
		}
		public async Task<QuartzClientResponse_Stream_PROTO> GetAllScheduledJobs(QuartzClientRequest_Stream_PROTO request)
		{
			var response = new QuartzClientResponse_Stream_PROTO();
			response.IsError = false;

			try
			{
				var jobKeys = await _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
				var scheduledJobs = new List<QuartzJob_PROTO>();

				foreach (var jobKey in jobKeys)
				{
					var jobDetail = await _scheduler.GetJobDetail(jobKey);
					var triggers = await _scheduler.GetTriggersOfJob(jobKey);
					var trigger = triggers.FirstOrDefault();
					var cron = string.Empty;
					if (trigger is ICronTrigger cronTrigger)
						cron = cronTrigger.CronExpressionString;			

					scheduledJobs.Add(new QuartzJob_PROTO
					{
						JobKey = jobDetail?.Key.Name,
						Schedule = cron,
						LastRunDate = trigger?.GetPreviousFireTimeUtc()?.ToTimestamp(),
                        ServiceInfo = new ServiceInfo
                        {
                            Server = _apiKeyProvider.ServerName,
                            Id = _apiKeyProvider.ServiceID,
                            Name = _apiKeyProvider.ServiceName
                        }
                    });
				}

				response.Jobs.AddRange(scheduledJobs);
			}
			catch (Exception ex)
			{
				response.IsError = true;
				response.ErrorMessage = ex.Message;
				_logger.LogError($"Error getting job:{request.Job.JobName}. ErrorMessage:{ex.Message}");
			}

			return response;
		}

		public async Task<QuartzClientResponse_Stream_PROTO> CreateJob(QuartzClientRequest_Stream_PROTO request)
		{
			var response = new QuartzClientResponse_Stream_PROTO();
			response.IsError = false;
			// validate request
			try
			{
				var newJob = new QuartzJob
				{
					Id = request.Job.JobKey,
					JobName = request.Job.JobName,
					AssemblyPath = request.Job.AssemblyPath,
					ClassName = request.Job.ClassName,
					ConfigName = request.Job.ConfigName,
					Schedule = request.Job.Schedule,
					Description = request.Job.Description
				};

				IJobDetail jobDetail = CreateJobDetail(newJob);
				ITrigger jobTrigger = CreateJobTrigger(newJob, jobDetail);

				await _scheduler.ScheduleJob(jobDetail, jobTrigger);
				_logger.LogInformation($"Job:{request.Job.JobKey} Created and Scheduled.");
			}
			catch (Exception ex)
			{
				response.IsError = true;
				response.ErrorMessage = ex.Message;
				_logger.LogError($"Error creating job:{request.Job.JobName}. ErrorMessage:{ex.Message}");
			}

			return response;
		}

		public async Task<QuartzClientResponse_Stream_PROTO> UpdateJob(QuartzClientRequest_Stream_PROTO request)
		{
			var response = new QuartzClientResponse_Stream_PROTO();

			// validate request
			var job = request.Job;
			response.IsError = false;

			try
			{
				var triggerKey = new TriggerKey(job.JobKey);
				var jobKey = GenerateJobKey(job.JobKey);

				var jobDetail = await _scheduler.GetJobDetail(jobKey);
				
                var newTrigger = TriggerBuilder.Create()
                        .WithIdentity(job.JobKey)
                        .ForJob(jobKey)
                        .WithCronSchedule(job.Schedule)
                        .Build();

                var trigger = await _scheduler.GetTrigger(triggerKey);
                // Reschedule the job with the new trigger
                await _scheduler.RescheduleJob(triggerKey, newTrigger);
			}
			catch (Exception ex)
			{
				response.IsError = true;
				response.ErrorMessage = ex.Message;
				_logger.LogError($"Error creating job:{job.JobName}. ErrorMessage:{ex.Message}");
			}

			return response;
		}

		public async Task<QuartzClientResponse_Stream_PROTO> CreateRunNowJob(QuartzClientRequest_Stream_PROTO request)
		{
			var response = new QuartzClientResponse_Stream_PROTO();

			var job = request.Job;
			try
			{
				// Ensure to use existing job if it exist to keep DisallowConcurrentExecution logic				
				var jobDetail = await _scheduler.GetJobDetail(GenerateJobKey(request.Job.JobKey));
				if (jobDetail != null)
				{
					await _scheduler.TriggerJob(jobDetail.Key);
					_logger.LogInformation($"Job:{job.JobKey} Created and Scheduled to run now.");
					return response;
				}

				// Create new 1 time run job
				var newJob = new QuartzJob
				{
					Id = job.JobKey,
					JobName = job.JobName,
					AssemblyPath = job.AssemblyPath,
					ClassName = job.ClassName,
					ConfigName = job.ConfigName,
					Schedule = job.Schedule,
					Description = job.Description
				};

				jobDetail = CreateJobDetail(newJob);

				ITrigger trigger = TriggerBuilder.Create()
						.WithIdentity($"{request.Job.JobKey}_RunNowTrigger_{DateTime.Now:yyyyMMddHHmmssfff}{DateTime.Now.Ticks}")
						.ForJob(jobDetail)
						.StartNow()
						.Build();

				await _scheduler.ScheduleJob(jobDetail, trigger);
				_logger.LogInformation($"Job:{job.JobKey} Created and Scheduled to run now.");
			}
			catch (Exception ex)
			{
				response.IsError = true;
				response.ErrorMessage = ex.Message;
				_logger.LogError($"Error creating run now job:{job.JobName}. ErrorMessage:{ex.Message}");
			}
			return response;
		}

		//public async void StartUpProcess()
		//{
		//	var jobDBResult = quartzDAL.GetEnabledJobs(_serviceID);
		//	if (jobDBResult.IsError)
		//	{
		//		_logger.LogError($"Error getting jobs quartzDAL.GetJobs(). ErrorMessage:{jobDBResult.ErrorMessage}");
		//		return;
		//	}

		//	foreach (var job in jobDBResult.Data)
		//	{
		//		try
		//		{
		//			IJobDetail jobDetail = CreateJobDetail(job);
		//			ITrigger jobTrigger = CreateJobTrigger(job, jobDetail);
		//			await _scheduler.ScheduleJob(jobDetail, jobTrigger);

		//			_logger.LogInformation($"Job:{job.JobName} Scheduled.");
		//		}
		//		catch (Exception ex)
		//		{
		//			_logger.LogError($"Error in StartUpProcess for job:{job.JobName}. ErrorMessage:{ex.Message}");
		//			continue;
		//		}


		//	}
		//}

		private IJobDetail CreateJobDetail(QuartzJob job)
		{
			Assembly a = Assembly.LoadFrom(job.AssemblyPath);
			var jobType = a.GetType(job.ClassName);

			return JobBuilder.Create(jobType)
					.WithIdentity(GenerateJobKey(job.Id))
					.UsingJobData("AssemblyPath", job.AssemblyPath)
					.UsingJobData("ClassName", job.ClassName)
					.UsingJobData("ConfigKey", job.ConfigName)
					.UsingJobData("JobId", job.Id)
					.UsingJobData("JobName", job.JobName)
					.WithDescription(job.Description)
					.DisallowConcurrentExecution()
					.Build();
		}

		private ITrigger CreateJobTrigger(QuartzJob job, IJobDetail jobDetail)
		{
			return TriggerBuilder.Create()
						.WithIdentity($"{jobDetail.Key}_Trigger")
						.ForJob(jobDetail)
						.WithCronSchedule(job.Schedule)
						.Build();
		}

		private JobKey GenerateJobKey(string jobKey)
		{
			return new JobKey(jobKey);
		}		
	}
}
