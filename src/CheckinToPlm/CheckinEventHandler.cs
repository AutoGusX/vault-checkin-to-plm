using System.Diagnostics;
using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

[assembly: ApiVersion("20.0")]
[assembly: ExtensionId("7c1d5a0e-3f0b-4f6e-9b55-2f6c6b0a4d11")]

namespace CheckinToPlm;

/// <summary>
/// Loaded by every process that hosts the Vault web services (Vault client, Inventor add-in, ...).
/// After a check-in it assigns/updates items and queues the Fusion Manage sync, per the user's settings.
/// </summary>
public class CheckinEventHandler : IWebServiceExtension
{
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(3);
    private static CheckinBatcher<WebServiceManager>? _batcher;

    public void OnLoad()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            if (process.ProcessName.Contains("JobProcessor", StringComparison.OrdinalIgnoreCase))
            {
                // Job Processor check-ins (property sync, visualization files) must not create items.
                Log.Info("Running inside the Job Processor; check-in automation is off in this process.");
                return;
            }

            _batcher = new CheckinBatcher<WebServiceManager>(QuietPeriod, ProcessBatch);
            DocumentService.CheckinFileEvents.Post += OnCheckinPost;
            Log.Info("Extension loaded; listening for check-ins.");
        }
        catch (Exception ex)
        {
            Log.Error("Failed to subscribe to check-in events", ex);
        }
    }

    private static void OnCheckinPost(object? sender, CheckinFileCommandEventArgs e)
    {
        // Runs inside the host's check-in call: stay fast and never throw.
        try
        {
            var file = e.ReturnValue;
            var manager = (sender as IWebService)?.WebServiceManager;
            if (file is null || manager is null)
            {
                Log.Info($"Check-in event ignored (file present: {file is not null}, service manager present: {manager is not null}).");
                return;
            }

            var settings = Settings.Load();
            if (!settings.AssignItem)
            {
                Log.Info($"Check-in: {file.Name} (file id {file.Id}); automation is switched off.");
                return;
            }

            if (!settings.IsInScope(file.Name))
            {
                Log.Info($"Check-in: {file.Name} (file id {file.Id}); file type not in scope.");
                return;
            }

            Log.Info($"Check-in: {file.Name} (file id {file.Id}, version {file.VerNum}); queued.");
            _batcher?.Add(manager, file.Id);
        }
        catch (Exception ex)
        {
            Log.Error("Check-in handler failed", ex);
        }
    }

    private static void ProcessBatch(WebServiceManager manager, IReadOnlyList<long> fileIds)
    {
        var settings = Settings.Load();
        if (!settings.AssignItem)
        {
            return;
        }

        Item[] items;
        try
        {
            items = ItemAssigner.Assign(manager, fileIds);
            Log.Info($"Assigned/updated {items.Length} item(s) for {fileIds.Count} file(s): {string.Join(", ", items.Select(i => i.ItemNum))}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Assign item failed for file id(s) {string.Join(", ", fileIds)}", ex);
            Notifier.Warn("Assign item failed after check-in. " + ex.Message);
            return;
        }

        if (!settings.SyncToFusionManage || items.Length == 0)
        {
            return;
        }

        try
        {
            var tasks = FusionManageSync.Queue(manager, items);
            Log.Info($"Queued {tasks.Count} Fusion Manage sync task(s): {string.Join("; ", tasks.Select(t => $"{t.Id} {t.Description}"))}.");
        }
        catch (Exception ex)
        {
            Log.Error("Queuing the Fusion Manage sync failed", ex);
            Notifier.Warn("Item assigned, but the Fusion Manage sync was not queued. " + ex.Message);
        }
    }
}
