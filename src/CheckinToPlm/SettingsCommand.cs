using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;

namespace CheckinToPlm;

/// <summary>Vault client commands: the settings dialog (Tools menu) and "Sync to Fusion Manage" on files.</summary>
public class SettingsCommand : IExplorerExtension
{
    public IEnumerable<CommandSite> CommandSites()
    {
        var command = new CommandItem("CheckinToPlm.Settings", "Check-in to PLM settings...")
        {
            Description = "Choose whether a check-in assigns an item and syncs it to Fusion Manage.",
            MultiSelectEnabled = false,
        };
        command.Execute += (_, _) => ShowSettings();

        var site = new CommandSite("CheckinToPlm.ToolsMenu", "Check-in to PLM")
        {
            Location = CommandSiteLocation.ToolsMenu,
            DeployAsPulldownMenu = false,
        };
        site.AddCommand(command);
        return [site, SyncFromFileCommand.CreateSite()];
    }

    private static void ShowSettings()
    {
        try
        {
            var settings = Settings.Load();
            using var dialog = new SettingsDialog(settings);
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            dialog.ApplyTo(settings);
            settings.Save();
            Log.Info($"Settings saved: assign item {settings.AssignItem}, sync {settings.SyncToFusionManage}, file types {string.Join(" ", settings.FileExtensions)}.");
        }
        catch (Exception ex)
        {
            Log.Error("Could not show or save settings", ex);
            MessageBox.Show("The settings could not be saved. " + ex.Message, "Check-in to PLM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public IEnumerable<CustomEntityHandler>? CustomEntityHandlers() => null;

    public IEnumerable<DetailPaneTab>? DetailTabs() => null;

    public IEnumerable<DockPanel>? DockPanels() => null;

    public IEnumerable<string>? HiddenCommands() => null;

    public void OnLogOff(IApplication application)
    {
    }

    public void OnLogOn(IApplication application)
    {
    }

    public void OnShutdown(IApplication application)
    {
    }

    public void OnStartup(IApplication application)
    {
    }
}
