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
