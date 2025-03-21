using Newtonsoft.Json.Linq;

public class EditorTabPage : TabPage
{
    public string FilePath { get; set; }
    public JObject JsonData { get; set; }
    public TreeView TreeView { get; set; }
    public TextBox RawJsonTextBox { get; set; }
    public string SelectedPropertyPath { get; set; }
    public string OriginalPropertyName { get; set; }

    // Add any other controls you want to store per tab here
}

public class EditorContext
{
    public JObject Json;
    public string FilePath;
    public SplitContainer EditorUI;
    public FlowLayoutPanel AnimationPanel { get; set; }
}

public class DarkTabControl : TabControl
{
    public Color TabBackgroundColor { get; set; } = Color.FromArgb(30, 30, 30);
    public Color TabBorderColor { get; set; } = Color.FromArgb(60, 60, 60);
    public Color TabTextColor { get; set; } = Color.White;
    public Color SelectedTabColor { get; set; } = Color.FromArgb(50, 50, 50);

    public DarkTabControl()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Fixed;
        ItemSize = new Size(150, 25);
        DoubleBuffered = true;

        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        BackColor = TabBackgroundColor;
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        TabPage tab = TabPages[e.Index];
        bool selected = (SelectedIndex == e.Index);
        Rectangle bounds = GetTabRect(e.Index);

        using (SolidBrush b = new SolidBrush(selected ? SelectedTabColor : TabBackgroundColor))
            e.Graphics.FillRectangle(b, bounds);

        using (Pen p = new Pen(TabBorderColor))
            e.Graphics.DrawRectangle(p, bounds);

        StringFormat stringFlags = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        using (Brush textBrush = new SolidBrush(TabTextColor))
            e.Graphics.DrawString(tab.Text, Font, textBrush, bounds, stringFlags);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using (SolidBrush b = new SolidBrush(TabBackgroundColor))
        {
            Rectangle contentRect = new Rectangle(DisplayRectangle.Left - 1, DisplayRectangle.Top - 1,
                                                  DisplayRectangle.Width + 2, DisplayRectangle.Height + 2);
            e.Graphics.FillRectangle(b, contentRect);
        }
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage tab)
            tab.BackColor = TabBackgroundColor;
    }
}
