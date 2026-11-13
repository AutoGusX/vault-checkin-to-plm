using System.Drawing;
using System.Windows.Forms;

namespace CheckinToPlm;

/// <summary>Shows a tray balloon without blocking the host application.</summary>
internal static class Notifier
{
    public static void Warn(string message) => Start(message, ToolTipIcon.Warning);

    public static void Info(string message) => Start(message, ToolTipIcon.Info);

    private static void Start(string message, ToolTipIcon kind)
    {
        try
        {
            var thread = new Thread(() => Show(message, kind)) { IsBackground = true, Name = "CheckinToPlm notification" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        catch (Exception ex)
        {
            Log.Error("Could not show a notification", ex);
        }
    }

    private static void Show(string message, ToolTipIcon kind)
    {
        try
        {
            using var context = new ApplicationContext();
            using var icon = new NotifyIcon { Icon = kind == ToolTipIcon.Warning ? SystemIcons.Warning : SystemIcons.Information, Visible = true, Text = "Check-in to PLM" };
            using var timer = new System.Windows.Forms.Timer { Interval = 10000 };
            timer.Tick += (_, _) => context.ExitThread();
            timer.Start();
            icon.ShowBalloonTip(8000, "Check-in to PLM", message, kind);
            Application.Run(context);
            icon.Visible = false;
        }
        catch (Exception ex)
        {
            Log.Error("Could not show a notification", ex);
        }
    }
}
