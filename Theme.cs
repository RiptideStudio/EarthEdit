public static class Theme
{
    public static Color Background = Color.FromArgb(30, 30, 30);
    public static Color Foreground = Color.White;
    public static Color Panel = Color.FromArgb(50, 50, 50);
    public static Color MenuBar = Panel;
    public static Color Button = Color.FromArgb(70, 70, 70);
    public static Color Highlight = Color.FromArgb(120, 120, 215);
    public static Color HighlightDark = Color.FromArgb(90, 90, 185);
    public static Color ButtonPressed = Color.FromArgb(45, 45, 45);

    public static void ApplyTheme(Control control)
    {
        if (control is TreeView)
        {
            control.BackColor = Theme.Background;
            control.ForeColor = Theme.Foreground;
        }

        if (control is Form)
            control.BackColor = Theme.Background;

        if (control is Panel || control is GroupBox)
            control.BackColor = Theme.Panel;

        if (control is Button)
        {
            control.BackColor = Theme.Button;
            control.ForeColor = Theme.Foreground;
            
            (control as Button).FlatStyle = FlatStyle.Flat;
            (control as Button).FlatAppearance.BorderColor = Theme.Highlight;
            (control as Button).FlatAppearance.MouseDownBackColor = Theme.ButtonPressed;
            (control as Button).FlatAppearance.BorderSize = 1;

        }

        if (control is ComboBox || control is CheckBox)
        {
            control.BackColor = Theme.Panel;
            control.ForeColor = Theme.Foreground;
        }

        if (control is TextBox)
        {
            (control as TextBox).BorderStyle = BorderStyle.FixedSingle;
        }

        if (control is Label)
        {
            control.ForeColor = Theme.Foreground;
        }

        if (control is MenuStrip menu)
        {
            menu.BackColor = Theme.MenuBar;
            menu.ForeColor = Theme.Foreground;
        }

        foreach (Control child in control.Controls)
            ApplyTheme(child);
    }

}

public class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkMenuColorTable()) { }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        Graphics g = e.Graphics;
        Rectangle rect = new Rectangle(Point.Empty, e.Item.Size);

        if (e.Item.Selected || e.Item.Pressed)
        {
            // 🔹 Make sure "File" never turns white
            g.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 65)), rect);
            e.Item.ForeColor = Color.WhiteSmoke;
        }
        else
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), rect);
            e.Item.ForeColor = Color.WhiteSmoke;
        }

        base.OnRenderMenuItemBackground(e);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.WhiteSmoke; // Force white text
        base.OnRenderItemText(e);
    }
}

public class DarkMenuColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => Color.FromArgb(60, 60, 65); // Dark hover
    public override Color MenuItemBorder => Color.FromArgb(106, 130, 173); // VS Purple (#6A0DAD)
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(50, 50, 55);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(50, 50, 55);
    public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48); // Dark background
    public override Color MenuBorder => Color.FromArgb(30, 30, 30);

    // Remove the left white strip (gradient margin)
    public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
    public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
}
