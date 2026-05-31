// Template: Optimizely/Episerver CMS 11 scheduled job — EPiServer 11.x on .NET Framework 4.x
// Packages: EPiServer.CMS.Core 11.x (net461)
// Placeholders: {{Namespace}} {{Job}}   ({{Job}} -> NightlySync)
//
// Derive from ScheduledJobBase; [ScheduledPlugIn] registers it in the admin UI.
// Honor Stop() for cancellable runs; report progress with OnStatusChanged; return a result string.
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace {{Namespace}}.Jobs
{
    [ScheduledPlugIn(
        DisplayName = "{{Job}}",
        GUID = "00000000-0000-0000-0000-000000000020",   // <-- replace with a unique GUID
        IntervalType = ScheduledIntervalType.Hours,
        IntervalLength = 24,
        Restartable = true)]
    public class {{Job}}Job : ScheduledJobBase
    {
        private bool _stopSignaled;

        public {{Job}}Job()
        {
            IsStoppable = true;   // lets an editor cancel a running job
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        public override string Execute()
        {
            OnStatusChanged($"Starting {nameof({{Job}}Job)}…");

            var processed = 0;
            foreach (var unit in GetWork())
            {
                if (_stopSignaled)
                    return $"Stopped by request after {processed} item(s).";

                ProcessUnit(unit);
                processed++;

                if (processed % 100 == 0)
                    OnStatusChanged($"Processed {processed} item(s)…");
            }

            return $"Completed: {processed} item(s) processed.";
        }

        private System.Collections.Generic.IEnumerable<object> GetWork()
        {
            yield break;   // <-- enumerate the work to perform
        }

        private void ProcessUnit(object unit)
        {
            // <-- per-item work
        }
    }
}
