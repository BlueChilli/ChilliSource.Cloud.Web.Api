using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using SerilogLib = Serilog;

namespace ChilliSource.Cloud.Web.Api
{
    public class DefaultExceptionLogger : ExceptionLogger
    {
        private SerilogLib.ILogger _logger;
        public SerilogLib.Events.LogEventLevel LogEventLevel { get; set; } = SerilogLib.Events.LogEventLevel.Error;

        public DefaultExceptionLogger(SerilogLib.ILogger logger)
        {
            _logger = logger;
        }

        public override void Log(ExceptionLoggerContext context)
        {
            if (this.ShouldLog(context))
            {
                _logger.Write(this.LogEventLevel, context.Exception, context.Exception.Message);
            }
        }

        public override Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            //The logging implementation should avoid blocking here.
            this.Log(context); 

            return Task.CompletedTask;
        }

        public override bool ShouldLog(ExceptionLoggerContext context)
        {
            return true;
        }
    }
}