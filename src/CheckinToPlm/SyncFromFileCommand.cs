using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;

namespace CheckinToPlm;

/// <summary>
/// "Sync to Fusion Manage" on files in the Vault client: finds the items tied to the
/// selected files and queues the connector sync, so the user never has to open the item.
/// </summary>
internal static class SyncFromFileCommand
{
    private const string Caption = "Sync to Fusion Manage";

    public static CommandSite CreateSite()
    {
        var command = new CommandItem("CheckinToPlm.SyncFromFile", "Sync to Fusion Manage")
        {
            Description = "Sync the items tied to the selected files to Fusion Manage.",
            NavigationTypes = [SelectionTypeId.File, SelectionTypeId.FileVersion],
            MultiSelectEnabled = true,
        };
        command.Execute += (_, e) => Execute(e);

        var site = new CommandSite("CheckinToPlm.FileContextMenu", "Check-in to PLM")
        {
            Location = CommandSiteLocation.FileContextMenu,
            DeployAsPulldownMenu = false,
        };
        site.AddCommand(command);
        return site;
    }

    private static void Execute(CommandItemEventArgs e)
    {
        try
        {
            var manager = e.Context.Application.Connection.WebServiceManager;
            var selections = e.Context.CurrentSelectionSet.ToList();

            Cursor.Current = Cursors.WaitCursor;
            var files = ResolveLatestFiles(manager, selections);
            if (files.Count == 0)
            {
                Show("Select one or more files to sync.", MessageBoxIcon.Information);
                return;
            }

            // master id -> item, and master id -> names of the selected files it is linked to
            var items = new Dictionary<long, Item>();
            var linkedFiles = new Dictionary<long, List<string>>();
            var withoutItem = new List<Autodesk.Connectivity.WebServices.File>();
            foreach (var file in files)
            {
                var linked = manager.ItemService.GetItemsByFileId(file.Id) ?? [];
                if (linked.Length == 0)
                {
                    withoutItem.Add(file);
                }

                foreach (var item in linked)
                {
                    Add(items, linkedFiles, item, file.Name);
                }
            }

            if (withoutItem.Count > 0)
            {
                var names = string.Join(", ", withoutItem.Take(5).Select(f => f.Name)) + (withoutItem.Count > 5 ? ", ..." : "");
                var question = withoutItem.Count == 1
                    ? $"{names} has no up-to-date item.\n\nAssign/update its item and sync it?"
                    : $"{withoutItem.Count} files have no up-to-date item ({names}).\n\nAssign/update their items and sync them?";
                var buttons = items.Count > 0 ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo;
                var answer = MessageBox.Show(question, Caption, buttons, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel || (answer == DialogResult.No && items.Count == 0))
                {
                    return;
                }

                if (answer == DialogResult.Yes)
                {
                    Cursor.Current = Cursors.WaitCursor;
                    ItemAssigner.Assign(manager, withoutItem.Select(f => f.Id).ToList());
                    foreach (var file in withoutItem)
                    {
                        foreach (var item in manager.ItemService.GetItemsByFileId(file.Id) ?? [])
                        {
                            Add(items, linkedFiles, item, file.Name);
                        }
                    }
                }
            }

            if (items.Count == 0)
            {
                Show("No items are tied to the selected files, so there is nothing to sync.", MessageBoxIcon.Information);
                return;
            }

            var chosen = items.Keys.ToList();
            if (items.Count > 1)
            {
                var choices = items.Values
                    .OrderBy(i => i.ItemNum, StringComparer.OrdinalIgnoreCase)
                    .Select(i => new ItemChoice(i.MasterId, i.ItemNum, i.Title ?? "", string.Join(", ", linkedFiles[i.MasterId])))
                    .ToList();
                using var picker = new ItemPickerDialog(choices);
                if (picker.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                chosen = picker.SelectedMasterIds.ToList();
            }

            Cursor.Current = Cursors.WaitCursor;
            // Sync the latest committed iteration of each item, as the native command does.
            var latest = manager.ItemService.GetLatestItemsByItemMasterIds(chosen.ToArray()) ?? [];
            var tasks = FusionManageSync.Queue(manager, latest);
            Log.Info($"Sync from file: queued {tasks.Count} task(s) for {latest.Length} item(s): {string.Join("; ", tasks.Select(t => $"{t.Id} {t.Description}"))}.");
            Notifier.Info(tasks.Count == 1
                ? $"Sync to Fusion Manage queued for item {latest[0].ItemNum}."
                : $"Sync to Fusion Manage queued: {tasks.Count} task(s) for {latest.Length} items.");
        }
        catch (Exception ex)
        {
            Log.Error("Sync from file failed", ex);
            Show("The sync could not be queued. " + ex.Message, MessageBoxIcon.Warning);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    /// <summary>Turns the selection (files by master ID, or specific versions) into latest file versions.</summary>
    private static List<Autodesk.Connectivity.WebServices.File> ResolveLatestFiles(WebServiceManager manager, List<ISelection> selections)
    {
        var masterIds = new List<long>();
        foreach (var selection in selections)
        {
            if (selection.TypeId == SelectionTypeId.File)
            {
                masterIds.Add(selection.Id);
            }
            else if (selection.TypeId == SelectionTypeId.FileVersion)
            {
                masterIds.Add(manager.DocumentService.GetFileById(selection.Id).MasterId);
            }
        }

        masterIds = masterIds.Distinct().ToList();
        return masterIds.Count == 0
            ? []
            : (manager.DocumentService.GetLatestFilesByMasterIds(masterIds.ToArray()) ?? []).ToList();
    }

    private static void Add(Dictionary<long, Item> items, Dictionary<long, List<string>> linkedFiles, Item item, string fileName)
    {
        items[item.MasterId] = item;
        if (!linkedFiles.TryGetValue(item.MasterId, out var names))
        {
            names = [];
            linkedFiles[item.MasterId] = names;
        }

        if (!names.Contains(fileName))
        {
            names.Add(fileName);
        }
    }

    private static void Show(string message, MessageBoxIcon icon) =>
        MessageBox.Show(message, Caption, MessageBoxButtons.OK, icon);
}
