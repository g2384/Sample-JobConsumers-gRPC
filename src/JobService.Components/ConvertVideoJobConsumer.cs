namespace JobService.Components
{
    using System.Threading.Tasks;
    using MassTransit;
    using Microsoft.Extensions.Logging;


    public class ConvertVideoJobConsumer :
        IJobConsumer<ConvertVideo>
    {
        readonly ILogger<ConvertVideoJobConsumer> _logger;

        public ConvertVideoJobConsumer(ILogger<ConvertVideoJobConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Run(JobContext<ConvertVideo> context)
        {
            var message = context.Job;

            _logger.LogInformation("Converting Video: {GroupId} {Path} {Index}/{Count}", message.GroupId, message.Path, message.Index, message.Count);
            
            await Task.Delay(100);

            await context.Publish<VideoConverted>(context.Job);
            
            _logger.LogInformation("Converted Video: {GroupId} {Path} {Index}/{Count}", message.GroupId, message.Path, message.Index, message.Count);
        }
    }
}