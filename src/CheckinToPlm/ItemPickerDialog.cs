using System.Drawing;
using System.Windows.Forms;

namespace CheckinToPlm;

/// <summary>One row in the picker: an item and the selected file(s) it is linked to.</summary>
internal sealed record ItemChoice(long MasterId, string Number, string Title, string Files);

/// <summary>Lets the user sync one, many, or all of the items tied to the selected files.</summary>
internal sealed class ItemPickerDialog : Form
{
    private readonly ListView _list = new()
    {
        View = View.Details,
        CheckBoxes = true,
        FullRowSelect = true,
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
        Size = new Size(620, 260),
    };

    private readonly Button _sync = new() { Text = "Sync", DialogResult = DialogResult.OK, AutoSize = true };

    public ItemPickerDialog(IReadOnlyList<ItemChoice> choices)
    {
        Text = "Sync to Fusion Manage";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        _list.Columns.Add("Item", 140);
        _list.Columns.Add("Title", 230);
        _list.Columns.Add("Linked file", 225);
        foreach (var choice in choices)
        {
            _list.Items.Add(new ListViewItem([choice.Number, choice.Title, choice.Files]) { Checked = true, Tag = choice });
        }

        _list.ItemChecked += (_, _) => UpdateSyncButton();

        var selectAll = new Button { Text = "Select all", AutoSize = true };
        var clear = new Button { Text = "Clear", AutoSize = true };
        selectAll.Click += (_, _) => SetAll(true);
        clear.Click += (_, _) => SetAll(false);
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

        var left = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0) };
        left.Controls.Add(selectAll);
        left.Controls.Add(clear);
        var right = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, Margin = new Padding(0) };
        right.Controls.Add(_sync);
        right.Controls.Add(cancel);
        var buttons = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.Controls.Add(left, 0, 0);
        buttons.Controls.Add(right, 1, 0);

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label
        {
            Text = "The selected files are tied to more than one item. Choose the items to sync.",
            AutoSize = true,
            Margin = new Padding(3, 0, 3, 8),
        });
        layout.Controls.Add(_list);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        AcceptButton = _sync;
        CancelButton = cancel;
        UpdateSyncButton();
    }

    public IReadOnlyList<long> SelectedMasterIds =>
        _list.CheckedItems.Cast<ListViewItem>().Select(row => ((ItemChoice)row.Tag!).MasterId).ToList();

    private void SetAll(bool value)
    {
        foreach (ListViewItem row in _list.Items)
        {
            row.Checked = value;
        }
    }

    private void UpdateSyncButton()
    {
        var count = _list.CheckedItems.Count;
        _sync.Enabled = count > 0;
        _sync.Text = count > 0 ? $"Sync {count} item{(count == 1 ? "" : "s")}" : "Sync";
    }
}
