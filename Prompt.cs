public static class Prompt
{
    public static string ShowDropdown(string title, string promptText, List<string> options)
    {
        Form prompt = new Form()
        {
            Width = 420,
            Height = 200,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false
        };

        Label textLabel = new Label()
        {
            Left = 10,
            Top = 10,
            Text = promptText,
            Width = 390
        };
        

        ComboBox comboBox = new ComboBox()
        {
            Left = 10,
            Top = 40,
            Width = 380,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        comboBox.Items.AddRange(options.ToArray());

        Label descriptionLabel = new Label()
        {
            Left = 10,
            Top = 70,
            Width = 390,
            Height = 40,
            AutoSize = false,
            Text = "",
            ForeColor = System.Drawing.Color.DimGray
        };

        // Update description on selection change
        comboBox.SelectedIndexChanged += (sender, e) =>
        {
            string selected = comboBox.SelectedItem?.ToString();
            if (ToolTipLibrary.ItemTooltips.TryGetValue(selected, out var tip))
                descriptionLabel.Text = tip;
            else
                descriptionLabel.Text = "";
        };

        Button confirmation = new Button()
        {
            Text = "OK",
            Left = 220,
            Width = 80,
            Top = 120,
            DialogResult = DialogResult.OK
        };

        Button cancel = new Button()
        {
            Text = "Cancel",
            Left = 310,
            Width = 80,
            Top = 120,
            DialogResult = DialogResult.Cancel
        };

        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(comboBox);
        prompt.Controls.Add(descriptionLabel);
        prompt.Controls.Add(confirmation);
        prompt.Controls.Add(cancel);
        prompt.AcceptButton = confirmation;
        prompt.CancelButton = cancel;

        // Pre-select first item if available
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;

        return prompt.ShowDialog() == DialogResult.OK ? comboBox.SelectedItem?.ToString() : null;
    }
}
