namespace JobService.Service;

using System;
using System.Threading;
using System.Threading.Tasks;
using JobService.Components;
using MassTransit;
using MassTransit.Contracts.JobService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class JobSubmissionService : BackgroundService
{
    readonly IBusControl _bus;
    readonly IServiceProvider _serviceProvider;
    readonly ILogger<JobSubmissionService> _logger;

    public JobSubmissionService(ILogger<JobSubmissionService> logger, IBusControl bus, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _bus = bus;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _bus.WaitForHealthStatus(BusHealthStatus.Healthy, stoppingToken);

        IRequestClient<ConvertVideo> client = _bus.CreateRequestClient<ConvertVideo>();

        await Task.Delay(1000); // waiting for the queue to setup

        const int total = 2;

        // method 1, use IRequestClient.GetResponse
        for (var i = 0; i < total; i++)
        {
            await Task.Delay(400, stoppingToken);

            var groupId = NewId.Next().ToString();

            var message = new ConvertVideo
            {
                GroupId = groupId,
                Index = i + 1,
                Count = total
            };

            Response<JobSubmissionAccepted> response = await client.GetResponse<JobSubmissionAccepted>(message);

            _logger.LogInformation("Job submitted: {Content} {Index}/{Count}", response.Message, i + 1, total);
        }

        await Task.Delay(1000); // waiting for the jobs to finish
        _logger.LogInformation("===============================================================");

        // method 2, use URI + context.RequestId = NewId.NextGuid();
        using (var scope = _serviceProvider.CreateScope())
        {
            // use SendEndpointProvider
            var provider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

            // or just use _bus.GetSendEndpoint(new Uri("queue:convert-job-queue")), without creating the scope
            var sender = await provider.GetSendEndpoint(new Uri("queue:convert-job-queue"));
            for (var i = 0; i < total; i++)
            {
                await Task.Delay(400, stoppingToken);

                var groupId = NewId.Next().ToString();

                var message = new ConvertVideo
                {
                    GroupId = groupId,
                    Index = i + 1,
                    Count = total
                };

                await sender.Send(message, context =>
                {
                    context.RequestId = NewId.NextGuid();
                }, new CancellationToken());

                _logger.LogInformation("Job submitted: {Content} {Index}/{Count}", message.GroupId, i + 1, total);
            }
        }

        await Task.Delay(1000); // waiting for the jobs to finish
        _logger.LogInformation("===============================================================");

        // method 3, use URI + Send<SubmitJob<T>>
        using (var scope = _serviceProvider.CreateScope())
        {
            // use SendEndpointProvider
            var provider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();

            // or just use _bus.GetSendEndpoint(new Uri("queue:convert-job-queue")), without creating the scope
            var sender = await provider.GetSendEndpoint(new Uri("queue:convert-job-queue"));
            for (var i = 0; i < total; i++)
            {
                await Task.Delay(400, stoppingToken);

                var groupId = NewId.Next().ToString();

                var message = new ConvertVideo
                {
                    GroupId = groupId,
                    Index = i + 1,
                    Count = total
                };

                await sender.Send<SubmitJob<ConvertVideo>>(new SubmitJobClass<ConvertVideo>()
                {
                    Job = message,
                    JobId = NewId.NextGuid()
                }, new CancellationToken());

                _logger.LogInformation("Job submitted: {Content} {Index}/{Count}", message.GroupId, i + 1, total);
            }
        }

        return;
    }

    public class SubmitJobClass<TJob> : SubmitJob<TJob>
        where TJob : class
    {
        public TJob Job { get; init; }

        public Guid JobId { get; init; }
    }
}