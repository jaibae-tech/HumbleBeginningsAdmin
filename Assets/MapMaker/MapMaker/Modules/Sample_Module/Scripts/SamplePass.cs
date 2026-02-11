using MapMaker.Core.Logging;
namespace MapMaker.Modules.Sample
{
    public sealed class SamplePass
    {
        public void Execute(LogEmitter emit)
        {
            emit(LogLevel.INFO, LogContext.Module, LogPhase.Generation, "SAMPLE", "Sample executed"); 
        } 
    } 
}
