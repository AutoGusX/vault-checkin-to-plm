using System.Drawing;
using System.Windows.Forms;

namespace CheckinToPlm;

/// <summary>The remembered checkboxes that control what happens after a check-in.</summary>
internal sealed class SettingsDialog : Form
{
    private readonly CheckBox _assign = new() { Text = "After check-in, assign/update item", AutoSize = true };
    private readonly CheckBox _sync = new() { Text = "Then sync the item to Fusion Manage", AutoSize = true, Margin = new Padding(22, 3, 3, 3) };
    private readonly TextBox _extensions = new() { Width = 320 };

    public SettingsDialog(Settings settings)
    {
        Text = "Check-in to PLM settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        _assign.Checked = settings.AssignItem;
        _sync.Checked = settings.SyncToFusionManage;
        _sync.Enabled = settings.AssignItem;
        _extensions.Text = string.Join(" ", settings.FileExtensions);
        _assign.CheckedChanged += (_, _) => _sync.Enabled = _assign.Checked;

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(_assign);
        layout.Controls.Add(_sync);
        layout.Controls.Add(new Label { Text = "File types", AutoSize = true, Margin = new Padding(3, 12, 3, 0) });
        layout.Controls.Add(_extensions);
        layout.Controls.Add(new Label
        {
            Text = "Applies to check-ins from the Vault client and from CAD add-ins.\nThe setting is saved for your Windows user.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 8, 3, 0),
        });
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    public void ApplyTo(Settings settings)
    {
        settings.AssignItem = _assign.Checked;
        settings.SyncToFusionManage = _sync.Checked;
        var extensions = Settings.ParseExtensions(_extensions.Text);
        if (extensions.Count > 0)
        {
            settings.FileExtensions = extensions;
        }
    }
}
