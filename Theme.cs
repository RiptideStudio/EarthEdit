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

        if (control is TextBox || control is ComboBox || control is CheckBox)
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

