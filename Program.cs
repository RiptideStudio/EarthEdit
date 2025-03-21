using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonEditorApp
{
    public partial class MainForm : Form
    {
        private JObject jsonData;
        private string currentFilePath;
        private Label fileNameLabel;
        private Button loadImageButton;
        private PixelArtPictureBox fileImagePictureBox;
        private SpriteAnimation spriteAnimation;
        private HashSet<string> expandedPaths = new HashSet<string>();
        private FlowLayoutPanel animationPanel;
        private List<SpriteAnimation> animationLayers = new List<SpriteAnimation>();
        private CheckBox enforcePresetCheckbox;
        private ComboBox presetSelector;
        private ToolStripMenuItem recentFilesMenuItem;
        public static ToolTip toolTip = new ToolTip();
        private TextBox nameTextBox;
        private TextBox valueTextBox;
        private ComboBox typeComboBox;
        private Label descriptionLabel;
        private string selectedPropertyPath = null;
        private string originalPropertyName = null;

        public MainForm()
        {
            InitializeComponent();
            InitializeNewJsonObject();

            var recent = RecentFiles.Load();
            if (recent.Count > 0 && File.Exists(recent[0]))
            {
                OpenFile(recent[0]);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Earth Editor";
            this.Size = new System.Drawing.Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Create the main layout panel to properly position MenuStrip & SplitContainer
            TableLayoutPanel layoutPanel = new TableLayoutPanel();
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.RowCount = 2;
            layoutPanel.ColumnCount = 1;
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Auto-size for MenuStrip
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Remaining space for SplitContainer
            this.Controls.Add(layoutPanel);

            // Create MenuStrip
            MenuStrip menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            this.MainMenuStrip = menuStrip;

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");

            ToolStripMenuItem newMenuItem = new ToolStripMenuItem("New", null, new EventHandler(NewFile));
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open", null, new EventHandler(OpenFile));
            ToolStripMenuItem saveMenuItem = new ToolStripMenuItem("Save", null, new EventHandler(SaveFile));
            ToolStripMenuItem saveAsMenuItem = new ToolStripMenuItem("Save As", null, new EventHandler(SaveFileAs));
            recentFilesMenuItem = new ToolStripMenuItem("Recent");
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                newMenuItem,
                openMenuItem,
                saveMenuItem,
                saveAsMenuItem,
                new ToolStripSeparator(),
                recentFilesMenuItem
            });

            menuStrip.Items.Add(fileMenu);
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { newMenuItem, openMenuItem, saveMenuItem, saveAsMenuItem });
            menuStrip.Items.Add(fileMenu);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
            layoutPanel.Controls.Add(menuStrip, 0, 0);

            // Create SplitContainer inside the layout panel
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(splitContainer, 0, 1); // Add to second row

            // Create Properties Panel (Left Panel)
            Panel propertiesPanel = new Panel();
            propertiesPanel.Dock = DockStyle.Fill;
            splitContainer.Panel1.Controls.Add(propertiesPanel);

            // Create a Panel to Wrap the Label and TreeView
            Panel treeContainerPanel = new Panel();
            treeContainerPanel.Dock = DockStyle.Fill; // Ensure it expands fully
            propertiesPanel.Controls.Add(treeContainerPanel);

            // JSON Properties Label
            FlowLayoutPanel headerPanel = new FlowLayoutPanel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.AutoSize = true;
            headerPanel.FlowDirection = FlowDirection.LeftToRight;
            propertiesPanel.Controls.Add(headerPanel);

            fileNameLabel = new Label();
            fileNameLabel.Text = "Editing: sample.json";
            fileNameLabel.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            fileNameLabel.Padding = new Padding(5);
            fileNameLabel.Dock = DockStyle.Top;
            headerPanel.Padding = new Padding(5);
            headerPanel.Controls.Add(fileNameLabel);

            // TreeView (Ensure It Is Visible)
            TreeView propertiesTreeView = new TreeView();
            propertiesTreeView.Dock = DockStyle.Fill;
            propertiesTreeView.AfterSelect += PropertiesTreeView_AfterSelect;
            propertiesTreeView.Name = "propertiesTreeView";
            treeContainerPanel.Controls.Add(propertiesTreeView);  // ✅ Now added to treeContainerPanel
            propertiesTreeView.AllowDrop = true;
            propertiesTreeView.ItemDrag += (s, e) => DoDragDrop(e.Item, DragDropEffects.Move);
            propertiesTreeView.DragEnter += (s, e) => e.Effect = DragDropEffects.Move;
            propertiesTreeView.DragOver += (s, e) => e.Effect = DragDropEffects.Move;
            propertiesTreeView.DragDrop += PropertiesTreeView_DragDrop;
            propertiesTreeView.ShowNodeToolTips = true;

            // Button panel for property management
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 40;
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            propertiesPanel.Controls.Add(buttonPanel);

            Button addPropertyButton = new Button();
            addPropertyButton.Text = "Add Property";
            addPropertyButton.AutoSize = true;
            addPropertyButton.Click += AddProperty_Click;
            buttonPanel.Controls.Add(addPropertyButton);

            Button removePropertyButton = new Button();
            removePropertyButton.Text = "Remove Property";
            removePropertyButton.AutoSize = true;
            removePropertyButton.Click += RemoveProperty_Click;
            buttonPanel.Controls.Add(removePropertyButton);

            // Editor panel (right)
            Panel editorPanel = new Panel();
            editorPanel.Dock = DockStyle.Fill;
            splitContainer.Panel2.Controls.Add(editorPanel);

            Label editorLabel = new Label();
            editorLabel.Text = "Property Editor";
            editorLabel.Dock = DockStyle.Top;
            editorLabel.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            editorLabel.Padding = new Padding(5);
            editorPanel.Controls.Add(editorLabel);

            Label propertyNameLabel = new Label();
            propertyNameLabel.Text = "Property Name:";
            propertyNameLabel.Location = new System.Drawing.Point(10, 40);
            editorPanel.Controls.Add(propertyNameLabel);

            TextBox propertyNameTextBox = new TextBox();
            propertyNameTextBox.Name = "propertyNameTextBox";
            propertyNameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            propertyNameTextBox.Location = new System.Drawing.Point(110, 40);
            propertyNameTextBox.Size = new System.Drawing.Size(300, 20);
            editorPanel.Controls.Add(propertyNameTextBox);

            Label propertyValueLabel = new Label();
            propertyValueLabel.Text = "Property Value:";
            propertyValueLabel.Location = new System.Drawing.Point(10, 70);
            editorPanel.Controls.Add(propertyValueLabel);

            TextBox propertyValueTextBox = new TextBox();
            propertyValueTextBox.Name = "propertyValueTextBox";
            propertyValueTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            propertyValueTextBox.Location = new System.Drawing.Point(110, 70);
            propertyValueTextBox.Size = new System.Drawing.Size(300, 100);
            propertyValueTextBox.Multiline = true;
            editorPanel.Controls.Add(propertyValueTextBox);

            Label propertyTypeLabel = new Label();
            propertyTypeLabel.Text = "Property Type:";
            propertyTypeLabel.Location = new System.Drawing.Point(10, 180);
            editorPanel.Controls.Add(propertyTypeLabel);

            ComboBox propertyTypeComboBox = new ComboBox();
            propertyTypeComboBox.Name = "propertyTypeComboBox";
            propertyTypeComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            propertyTypeComboBox.Location = new System.Drawing.Point(110, 180);
            propertyTypeComboBox.Size = new System.Drawing.Size(300, 20);
            propertyTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            propertyTypeComboBox.Items.AddRange(new string[] { "String", "Number", "Boolean", "Object", "Array", "Null" });
            propertyTypeComboBox.SelectedIndex = 0;
            editorPanel.Controls.Add(propertyTypeComboBox);

            // Raw JSON display
            Label rawJsonLabel = new Label();
            rawJsonLabel.Text = "Raw JSON:";
            rawJsonLabel.Location = new System.Drawing.Point(10, 250);
            editorPanel.Controls.Add(rawJsonLabel);

            TextBox rawJsonTextBox = new TextBox();
            rawJsonTextBox.Name = "rawJsonTextBox";
            rawJsonTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            rawJsonTextBox.Location = new System.Drawing.Point(10, 280);
            rawJsonTextBox.Size = new System.Drawing.Size(450, 200);
            rawJsonTextBox.Multiline = true;
            rawJsonTextBox.ScrollBars = ScrollBars.Vertical;
            rawJsonTextBox.ReadOnly = true;
            editorPanel.Controls.Add(rawJsonTextBox);

            Button formatJsonButton = new Button();
            formatJsonButton.Text = "Format JSON";
            formatJsonButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            formatJsonButton.Location = new System.Drawing.Point(10, 490);
            formatJsonButton.Size = new System.Drawing.Size(100, 30);
            formatJsonButton.Click += FormatJson_Click;
            editorPanel.Controls.Add(formatJsonButton);

            Button updateFromRawButton = new Button();
            updateFromRawButton.Text = "Update From Raw";
            updateFromRawButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            updateFromRawButton.Location = new System.Drawing.Point(120, 490);
            updateFromRawButton.Size = new System.Drawing.Size(120, 30);
            updateFromRawButton.Click += UpdateFromRaw_Click;
            editorPanel.Controls.Add(updateFromRawButton);

            propertyNameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            propertyValueTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            propertyTypeComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rawJsonTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            enforcePresetCheckbox = new CheckBox();
            enforcePresetCheckbox.Name = "enforcePresetCheckbox";
            enforcePresetCheckbox.Text = "Free Edit";
            enforcePresetCheckbox.Checked = false;
            headerPanel.Controls.Add(enforcePresetCheckbox);
            Label presetLabel = new Label();
            presetLabel.Text = "Preset:";
            presetLabel.AutoSize = true;
            presetLabel.TextAlign = ContentAlignment.MiddleLeft;
            presetLabel.Padding = new Padding(5, 6, 0, 0); // optional for spacing/alignment
            headerPanel.Controls.Add(presetLabel);

            presetSelector = new ComboBox();
            presetSelector.Name = "presetSelector";
            presetSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            presetSelector.Items.AddRange(JsonSchemas.Presets.Keys.ToArray());
            presetSelector.SelectedIndexChanged += PresetSelector_SelectedIndexChanged;
            presetSelector.SelectedItem = "Item"; // 👈 Default
            headerPanel.Controls.Add(presetSelector);

            Panel imagePanel = new Panel();
            imagePanel.Dock = DockStyle.Right;
            imagePanel.Width = 300;
            imagePanel.Padding = new Padding(0, 0, 100, 0);
            editorPanel.Controls.Add(imagePanel);

            animationPanel = new FlowLayoutPanel();
            animationPanel.Dock = DockStyle.Right;
            animationPanel.Width = 300;
            animationPanel.FlowDirection = FlowDirection.TopDown;
            animationPanel.AutoScroll = true;
            animationPanel.WrapContents = false;
            editorPanel.Controls.Add(animationPanel);

            nameTextBox = new TextBox();
            nameTextBox.Name = "propertyNameTextBox";
            editorPanel.Controls.Add(nameTextBox);

            valueTextBox = new TextBox();
            valueTextBox.Name = "propertyValueTextBox";
            editorPanel.Controls.Add(valueTextBox);

            typeComboBox = new ComboBox();
            typeComboBox.Name = "propertyTypeComboBox";
            editorPanel.Controls.Add(typeComboBox);

            // Add tooltips
            toolTip.SetToolTip(enforcePresetCheckbox, "Whether you can edit the JSON files freely, unbound by Earthward data schemas");
            toolTip.SetToolTip(presetSelector, "The type of data you want to make. This defines what properties you can and can't add");

            // Update recent files
            UpdateRecentFilesMenu();

            // Color scheme
            Theme.ApplyTheme(Controls.Owner);
        }

        private void UpdateRecentFilesMenu()
        {
            recentFilesMenuItem.DropDownItems.Clear();
            var files = RecentFiles.Load();

            if (files.Count == 0)
            {
                recentFilesMenuItem.DropDownItems.Add("(No recent files)").Enabled = false;
                return;
            }

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                var item = new ToolStripMenuItem(fileName)
                {
                    ToolTipText = filePath
                };

                item.Click += (s, e) =>
                {
                    if (File.Exists(filePath))
                    {
                        OpenFile(filePath);
                    }
                    else
                    {
                        MessageBox.Show("File not found: " + filePath, "Missing File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        var updatedList = RecentFiles.Load();
                        updatedList.Remove(filePath);
                        RecentFiles.Save(updatedList);
                        UpdateRecentFilesMenu();
                    }
                };

                recentFilesMenuItem.DropDownItems.Add(item);
            }
        }

        private void PresetSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            string selectedPreset = combo.SelectedItem.ToString();
        }

        private void InitializeNewJsonObject()
        {
            jsonData = new JObject();
            currentFilePath = null;
            UpdateTreeView();
            UpdateRawJsonDisplay();
        }

        private void SaveExpandedNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.IsExpanded && node.Tag is string path)
                    expandedPaths.Add(path);
                SaveExpandedNodes(node.Nodes);
            }
        }

        private void RestoreExpandedNodes(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is string path && expandedPaths.Contains(path))
                    node.Expand();
                RestoreExpandedNodes(node.Nodes);
            }
        }

        private void UpdateTreeView()
        {
            TreeView treeView = this.Controls.Find("propertiesTreeView", true)[0] as TreeView;

            expandedPaths.Clear();
            SaveExpandedNodes(treeView.Nodes);

            treeView.BeginUpdate();
            treeView.Nodes.Clear();
            PopulateTreeView(treeView.Nodes, jsonData);
            RestoreExpandedNodes(treeView.Nodes);
            treeView.EndUpdate();
        }


        private void PropertiesTreeView_DragDrop(object sender, DragEventArgs e)
        {
            TreeView tree = sender as TreeView;
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = tree.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = tree.GetNodeAt(pt);

            if (draggedNode != null && targetNode != null && draggedNode != targetNode)
            {
                draggedNode.Remove();
                targetNode.Nodes.Add(draggedNode);
                targetNode.Expand();

                // TODO: Update jsonData to reflect new hierarchy
                // You’ll need to reassign the property or array element in the underlying JSON
            }
        }

        private void PopulateTreeView(TreeNodeCollection nodes, JToken token, string path = "")
        {
            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    string currentPath = string.IsNullOrEmpty(path) ? property.Name : path + "." + property.Name;
                    TreeNode node = new TreeNode(property.Name);
                    node.Tag = currentPath;
                    nodes.Add(node);

                    if (ToolTipLibrary.ItemTooltips.TryGetValue(property.Name, out string tip))
                    {
                        node.ToolTipText = tip;
                    }

                    if (property.Value is JObject || property.Value is JArray)
                    {
                        PopulateTreeView(node.Nodes, property.Value, currentPath);
                    }
                    else
                    {
                        string valueText = GetDisplayValueForTreeNode(property.Value);
                        node.Text = $"{property.Name}: {valueText}";
                    }
                }
            }
            else if (token is JArray array)
            {
                for (int i = 0; i < array.Count; i++)
                {
                    string currentPath = $"{path}[{i}]";
                    TreeNode node = new TreeNode($"[{i}]");
                    node.Tag = currentPath;
                    nodes.Add(node);

                    if (array[i] is JObject || array[i] is JArray)
                    {
                        PopulateTreeView(node.Nodes, array[i], currentPath);
                    }
                    else
                    {
                        string valueText = GetDisplayValueForTreeNode(array[i]);
                        node.Text = $"[{i}]: {valueText}";
                    }
                }
            }
        }

        private string GetDisplayValueForTreeNode(JToken token)
        {
            if (token is JValue value)
            {
                if (value.Type == JTokenType.String)
                    return $"\"{value.Value}\"";
                else if (value.Value == null)
                    return "null";
                else
                    return value.Value.ToString();
            }
            else if (token is JObject)
            {
                return "{...}";
            }
            else if (token is JArray)
            {
                return "[...]";
            }
            return string.Empty;
        }

        private void PropertiesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                string path = e.Node.Tag.ToString();
                JToken token = GetTokenAtPath(path);

                if (token != null)
                {
                    nameTextBox = this.Controls.Find("propertyNameTextBox", true)[0] as TextBox;
                    valueTextBox = this.Controls.Find("propertyValueTextBox", true)[0] as TextBox;
                    typeComboBox = this.Controls.Find("propertyTypeComboBox", true)[0] as ComboBox;

                    // Get the property name
                    string[] pathParts = path.Split('.');
                    string propertyName = pathParts[pathParts.Length - 1];

                    // Check if it's an array index
                    if (propertyName.StartsWith("[") && propertyName.EndsWith("]"))
                    {
                        // For arrays, use the index as name
                        nameTextBox.Text = propertyName;
                        nameTextBox.ReadOnly = true;
                    }
                    else
                    {
                        nameTextBox.Text = propertyName;
                        nameTextBox.ReadOnly = false;
                    }

                    selectedPropertyPath = path;
                    originalPropertyName = propertyName;

                    nameTextBox.TextChanged -= PropertyEditor_Changed;
                    valueTextBox.TextChanged -= PropertyEditor_Changed;
                    typeComboBox.SelectedIndexChanged -= PropertyEditor_Changed;

                    nameTextBox.TextChanged += PropertyEditor_Changed;
                    valueTextBox.TextChanged += PropertyEditor_Changed;
                    typeComboBox.SelectedIndexChanged += PropertyEditor_Changed;

                    // Set the value and type
                    if (token is JValue jValue)
                    {
                        valueTextBox.Text = jValue.Value?.ToString() ?? "";

                        switch (jValue.Type)
                        {
                            case JTokenType.String:
                                typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf("String");
                                break;
                            case JTokenType.Integer:
                            case JTokenType.Float:
                                typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf("Number");
                                break;
                            case JTokenType.Boolean:
                                typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf("Boolean");
                                break;
                            case JTokenType.Null:
                                typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf("Null");
                                break;
                        }
                    }
                    else if (token is JObject)
                    {
                        valueTextBox.Text = JsonConvert.SerializeObject(token, Formatting.Indented);
                        typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf("Object");
                    }
                    else if (token is JArray)
                    {
                        valueTextBox.Text = JsonConvert.SerializeObject(token, Formatting.Indented);
                        typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf("Array");
                    }
                }
            }
        }

        private JToken GetTokenAtPath(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    return jsonData;

                if (path.Contains("["))
                {
                    // Handle array indices in path
                    string[] parts = path.Split('.');
                    JToken current = jsonData;

                    foreach (var part in parts)
                    {
                        if (part.Contains("["))
                        {
                            int openBracket = part.IndexOf('[');
                            string propertyName = part.Substring(0, openBracket);

                            if (!string.IsNullOrEmpty(propertyName))
                                current = current[propertyName];

                            // Extract all array indices
                            int pos = openBracket;
                            while (pos < part.Length && part[pos] == '[')
                            {
                                int closeBracket = part.IndexOf(']', pos);
                                if (closeBracket == -1) break;

                                string indexStr = part.Substring(pos + 1, closeBracket - pos - 1);
                                if (int.TryParse(indexStr, out int index))
                                {
                                    current = current[index];
                                }

                                pos = closeBracket + 1;
                            }
                        }
                        else
                        {
                            current = current[part];
                        }
                    }

                    return current;
                }
                else
                {
                    // Simple dot notation path
                    return jsonData.SelectToken(path);
                }
            }
            catch
            {
                return null;
            }
        }

        private void UpdateProperty_Click(object sender, EventArgs e)
        {
            TreeView treeView = this.Controls.Find("propertiesTreeView", true)[0] as TreeView;

            if (treeView.SelectedNode == null || treeView.SelectedNode.Tag == null)
            {
                MessageBox.Show("Please select a property to update.", "No Property Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TextBox nameTextBox = this.Controls.Find("propertyNameTextBox", true)[0] as TextBox;
            TextBox valueTextBox = this.Controls.Find("propertyValueTextBox", true)[0] as TextBox;
            ComboBox typeComboBox = this.Controls.Find("propertyTypeComboBox", true)[0] as ComboBox;

            string path = treeView.SelectedNode.Tag.ToString();
            string newName = nameTextBox.Text.Trim();
            string valueText = valueTextBox.Text;
            string selectedType = typeComboBox.SelectedItem.ToString();

            // ✅ Schema enforcement check
            if (enforcePresetCheckbox?.Checked == true && presetSelector?.SelectedItem != null)
            {
                string selectedPreset = presetSelector.SelectedItem.ToString();

                if (JsonSchemas.Presets.TryGetValue(selectedPreset, out var allowedFields))
                {
                    bool isValid = allowedFields.Any(f => f.name == newName);
                    if (!isValid)
                    {
                        MessageBox.Show($"'{newName}' is not a valid property for preset '{selectedPreset}'.", "Invalid Property", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            // Validate name
            if (string.IsNullOrWhiteSpace(newName) && !newName.StartsWith("["))
            {
                MessageBox.Show("Property name cannot be empty.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Create the new value based on selected type
                JToken newValue;
                switch (selectedType)
                {
                    case "String":
                        newValue = new JValue(valueText);
                        break;
                    case "Number":
                        if (double.TryParse(valueText, out double numValue))
                            newValue = new JValue(numValue);
                        else
                            newValue = new JValue(0);
                        break;
                    case "Boolean":
                        if (bool.TryParse(valueText, out bool boolValue))
                            newValue = new JValue(boolValue);
                        else
                            newValue = new JValue(false);
                        break;
                    case "Object":
                        try
                        {
                            newValue = JObject.Parse(valueText);
                        }
                        catch
                        {
                            newValue = new JObject();
                        }
                        break;
                    case "Array":
                        try
                        {
                            newValue = JArray.Parse(valueText);
                        }
                        catch
                        {
                            newValue = new JArray();
                        }
                        break;
                    case "Null":
                        newValue = JValue.CreateNull();
                        break;
                    default:
                        newValue = new JValue(valueText);
                        break;
                }

                // Update the JSON data
                UpdateJsonProperty(path, newName, newValue);

                // Update UI
                UpdateTreeView();
                UpdateRawJsonDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PropertyEditor_Changed(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPropertyPath)) return;

            var nameTextBox = this.Controls.Find("propertyNameTextBox", true)[0] as TextBox;
            var valueTextBox = this.Controls.Find("propertyValueTextBox", true)[0] as TextBox;
            var typeComboBox = this.Controls.Find("propertyTypeComboBox", true)[0] as ComboBox;

            string newName = nameTextBox.Text.Trim();
            string valueText = valueTextBox.Text;
            string selectedType = typeComboBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(newName)) return;

            try
            {
                // Create new JToken based on type
                JToken newValue = selectedType switch
                {
                    "String" => new JValue(valueText),
                    "Number" => double.TryParse(valueText, out var num) ? new JValue(num) : new JValue(0),
                    "Boolean" => bool.TryParse(valueText, out var b) ? new JValue(b) : new JValue(false),
                    "Object" => JObject.Parse(valueText),
                    "Array" => JArray.Parse(valueText),
                    "Null" => JValue.CreateNull(),
                    _ => new JValue(valueText)
                };

                // Apply it
                UpdateJsonProperty(selectedPropertyPath, newName, newValue);
                UpdateRawJsonDisplay();

                TreeView treeView = this.Controls.Find("propertiesTreeView", true)[0] as TreeView;
                TreeNode selectedNode = treeView.SelectedNode;

                if (selectedNode != null)
                {
                    string displayValue = GetDisplayValueForTreeNode(newValue);
                    selectedNode.Text = $"{newName}: {displayValue}";
                    selectedNode.Name = newName;
                    selectedNode.Tag = selectedPropertyPath;
                }

                // Update path tracking
                selectedPropertyPath = selectedPropertyPath.Contains(".")
                    ? selectedPropertyPath.Substring(0, selectedPropertyPath.LastIndexOf('.') + 1) + newName
                    : newName;
            }
            catch
            {
                // Optional: silently fail while typing
            }
        }

        private void UpdateJsonProperty(string path, string newName, JToken newValue)
        {
            // For array items, we just update the value
            if (path.EndsWith("]"))
            {
                JToken token = GetTokenAtPath(path);
                if (token != null && token.Parent != null)
                {
                    token.Replace(newValue);
                }
                return;
            }

            // For regular properties
            string parentPath = "";
            string currentName = newName;

            // Extract parent path and current property name
            int lastDotIndex = path.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                parentPath = path.Substring(0, lastDotIndex);
                currentName = path.Substring(lastDotIndex + 1);
            }

            JToken parentToken = string.IsNullOrEmpty(parentPath) ? jsonData : GetTokenAtPath(parentPath);

            if (parentToken is JObject parentObj)
            {
                // Remove the old property
                parentObj.Remove(currentName);

                // Add with new name and value
                parentObj.Add(newName, newValue);
            }
        }

        private void AddProperty_Click(object sender, EventArgs e)
        {
            TreeView treeView = this.Controls.Find("propertiesTreeView", true)[0] as TreeView;
            ComboBox presetSelector = this.Controls.Find("presetSelector", true)[0] as ComboBox;
            CheckBox enforcePresetCheckbox = this.Controls.Find("enforcePresetCheckbox", true)[0] as CheckBox;

            string parentPath = "";
            if (treeView.SelectedNode != null && treeView.SelectedNode.Tag != null)
            {
                string selectedPath = treeView.SelectedNode.Tag.ToString();
                JToken token = GetTokenAtPath(selectedPath);

                if (token is JObject)
                {
                    parentPath = selectedPath;
                }
                else if (token.Parent is JObject)
                {
                    int lastDotIndex = selectedPath.LastIndexOf('.');
                    if (lastDotIndex >= 0)
                    {
                        parentPath = selectedPath.Substring(0, lastDotIndex);
                    }
                }
            }

            JObject parentObj = string.IsNullOrEmpty(parentPath) ? jsonData : GetTokenAtPath(parentPath) as JObject;
            if (parentObj == null)
            {
                MessageBox.Show("Selected item cannot have properties added to it.", "Cannot Add Property", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newPropertyName = null;

            if (enforcePresetCheckbox?.Checked == false && presetSelector?.SelectedItem != null)
            {
                string selectedPreset = presetSelector.SelectedItem.ToString();
                if (JsonSchemas.Presets.TryGetValue(selectedPreset, out var fields))
                {
                    var available = fields.Select(f => f.name).Where(name => !parentObj.ContainsKey(name)).ToList();
                    if (available.Count == 0)
                    {
                        MessageBox.Show("All allowed properties already exist.", "Nothing To Add", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    newPropertyName = Prompt.ShowDropdown("Add Property", $"Add a variable to your Earthward {presetSelector.SelectedItem}", available);
                    if (string.IsNullOrEmpty(newPropertyName)) return;

                    // Add default value from schema
                    var field = fields.First(f => f.name == newPropertyName);
                    parentObj[newPropertyName] = JToken.FromObject(field.defaultValue ?? "");
                }
            }
            else 
            {
                // Add a generic "NewProperty" with incremental suffix
                int counter = 1;
                newPropertyName = "NewProperty";

                while (parentObj.ContainsKey(newPropertyName))
                    newPropertyName = $"NewProperty{counter++}";

                parentObj[newPropertyName] = "";
            }

            // Update UI
            UpdateTreeView();
            UpdateRawJsonDisplay();
            string newPath = string.IsNullOrEmpty(parentPath) ? newPropertyName : parentPath + "." + newPropertyName;
            SelectNodeByPath(newPath);
        }

        private void SelectNodeByPath(string path)
        {
            TreeView treeView = this.Controls.Find("propertiesTreeView", true)[0] as TreeView;

            foreach (TreeNode node in treeView.Nodes)
            {
                TreeNode found = FindNodeByPath(node, path);
                if (found != null)
                {
                    treeView.SelectedNode = found;
                    break;
                }
            }
        }

        private TreeNode FindNodeByPath(TreeNode node, string path)
        {
            if (node.Tag != null && node.Tag.ToString() == path)
                return node;

            foreach (TreeNode child in node.Nodes)
            {
                TreeNode found = FindNodeByPath(child, path);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void RemoveProperty_Click(object sender, EventArgs e)
        {
            TreeView treeView = this.Controls.Find("propertiesTreeView", true)[0] as TreeView;

            if (treeView.SelectedNode == null || treeView.SelectedNode.Tag == null)
            {
                MessageBox.Show("Please select a property to remove.", "No Property Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string path = treeView.SelectedNode.Tag.ToString();

            // Confirm deletion
            if (MessageBox.Show($"Are you sure you want to remove the selected property?", "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                RemoveJsonProperty(path);

                // Update UI
                UpdateTreeView();
                UpdateRawJsonDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing property: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveJsonProperty(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (path.Contains("[") && path.EndsWith("]"))
            {
                // Handle array item removal
                int openBracketIndex = path.LastIndexOf('[');
                int closeBracketIndex = path.LastIndexOf(']');

                if (openBracketIndex >= 0 && closeBracketIndex > openBracketIndex)
                {
                    string indexStr = path.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
                    if (int.TryParse(indexStr, out int index))
                    {
                        string arrayPath = path.Substring(0, openBracketIndex);
                        JToken arrayToken = string.IsNullOrEmpty(arrayPath) ? jsonData : GetTokenAtPath(arrayPath);

                        if (arrayToken is JArray array && index >= 0 && index < array.Count)
                        {
                            array.RemoveAt(index);
                        }
                    }
                }
            }
            else
            {
                // Handle regular property removal
                int lastDotIndex = path.LastIndexOf('.');

                if (lastDotIndex >= 0)
                {
                    string parentPath = path.Substring(0, lastDotIndex);
                    string propertyName = path.Substring(lastDotIndex + 1);

                    JToken parentToken = GetTokenAtPath(parentPath);

                    if (parentToken is JObject parentObj)
                    {
                        parentObj.Remove(propertyName);
                    }
                }
                else
                {
                    // Root level property
                    jsonData.Remove(path);
                }
            }
        }

        private void UpdateRawJsonDisplay()
        {
            TextBox rawJsonTextBox = this.Controls.Find("rawJsonTextBox", true)[0] as TextBox;
            rawJsonTextBox.Text = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
        }

        private void FormatJson_Click(object sender, EventArgs e)
        {
            TextBox rawJsonTextBox = this.Controls.Find("rawJsonTextBox", true)[0] as TextBox;

            try
            {
                // Parse and reformat
                string formattedJson = JsonConvert.SerializeObject(
                    JsonConvert.DeserializeObject(rawJsonTextBox.Text),
                    Formatting.Indented
                );

                rawJsonTextBox.Text = formattedJson;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error formatting JSON: {ex.Message}", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateFromRaw_Click(object sender, EventArgs e)
        {
            TextBox rawJsonTextBox = this.Controls.Find("rawJsonTextBox", true)[0] as TextBox;

            try
            {
                // Parse the raw JSON
                JObject newJsonData = JObject.Parse(rawJsonTextBox.Text);

                // Update the data
                jsonData = newJsonData;

                // Update UI
                UpdateTreeView();
                UpdateRawJsonDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing JSON: {ex.Message}", "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NewFile(object sender, EventArgs e)
        {
            if (MessageBox.Show("Create a new file? Any unsaved changes will be lost.", "New File", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                InitializeNewJsonObject();
            }
        }

        private void OpenFile(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.Title = "Open JSON File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OpenFile(openFileDialog.FileName);
                }
            }
        }

        private void OpenFile(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);
                jsonData = JObject.Parse(fileContent);
                currentFilePath = filePath;
                fileNameLabel.Text = "Editing: " + Path.GetFileName(currentFilePath);
                RecentFiles.Add(currentFilePath);

                // Detect matching PNG files
                string jsonDirectory = Path.GetDirectoryName(currentFilePath);
                string jsonFileNameWithoutExt = Path.GetFileNameWithoutExtension(currentFilePath);

                // Get base image (e.g., Helmet.png)
                string baseImagePath = Path.Combine(jsonDirectory, jsonFileNameWithoutExt + ".png");

                // Get all images like Helmet_Head.png, Helmet_Body.png, etc.
                string[] extraImagePaths = Directory.GetFiles(jsonDirectory, jsonFileNameWithoutExt + "_*.png");

                spriteAnimation?.Stop(); // If you had a single one before

                // Stop and clear previous animations
                foreach (var anim in animationLayers)
                    anim.Stop();

                animationLayers.Clear();
                animationPanel.Controls.Clear();

                List<string> allImages = new List<string>();

                if (File.Exists(baseImagePath))
                    allImages.Add(baseImagePath);

                allImages.AddRange(extraImagePaths.OrderBy(p => p));

                if (allImages.Count > 0)
                {
                    foreach (string imagePath in allImages)
                    {
                        PixelArtPictureBox picBox = new PixelArtPictureBox();
                        picBox.Size = new Size(256, 256);
                        picBox.Margin = new Padding(5);
                        picBox.BackColor = Color.DarkGray; 

                        animationPanel.Controls.Add(picBox);

                        var animation = new SpriteAnimation(picBox, imagePath);
                        animationLayers.Add(animation);
                    }
                }
                else
                {
                    // no image
                }

                RecentFiles.Add(filePath);
                UpdateRecentFilesMenu();

                UpdateTreeView();
                UpdateRawJsonDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFile(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs(sender, e);
            }
            else
            {
                try
                {
                    File.WriteAllText(currentFilePath, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
                    MessageBox.Show("File saved successfully.", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveFileAs(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.Title = "Save JSON File";
            saveFileDialog.DefaultExt = "json";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
                    currentFilePath = saveFileDialog.FileName;
                    MessageBox.Show("File saved successfully.", "Save Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveFile(null, null); // Call your existing Save function
                return true; // Mark as handled
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}