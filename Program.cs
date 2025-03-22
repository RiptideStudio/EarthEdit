using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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
        private JProperty selectedProperty;

        private string originalPropertyName = null;
        private TabControl fileTabControl;
        private string selectedPath = null;

        public MainForm()
        {
            this.Icon = new Icon("Icon.ico");
            InitializeComponent();

            var recent = RecentFiles.Load();
            if (recent.Count > 0 && File.Exists(recent[0]))
            {
                OpenFileInNewTab(recent[0]);
            }
            else
            {
                CreateNewEditorTab("Untitled", null);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Earth Editor";
            this.Size = new System.Drawing.Size(1280, 720);
            this.StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel layoutPanel = new TableLayoutPanel();
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.RowCount = 2;
            layoutPanel.ColumnCount = 1;
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            this.Controls.Add(layoutPanel);

            MenuStrip menuStrip = new MenuStrip();
            menuStrip.Dock = DockStyle.Top;
            this.MainMenuStrip = menuStrip;

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem newMenuItem = new ToolStripMenuItem("New Item");
            ToolStripMenuItem newTab = new ToolStripMenuItem("New Tab", null, new EventHandler(NewTab));
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("Open Item", null, new EventHandler(OpenFile));
            ToolStripMenuItem saveMenuItem = new ToolStripMenuItem("Save", null, new EventHandler(SaveFile));
            ToolStripMenuItem saveAsMenuItem = new ToolStripMenuItem("Save As", null, new EventHandler(SaveFileAs));
            ToolStripMenuItem openInNewTabMenuItem = new ToolStripMenuItem("Open in New Tab", null, new EventHandler(OpenFileInNewTabMenu));
            recentFilesMenuItem = new ToolStripMenuItem("Recent");

            foreach (var schema in JsonSchemas.Presets.Keys)
            {
                var schemaItem = new ToolStripMenuItem(schema);
                schemaItem.Click += (s, e) => CreateNewFileFromSchema(schema);
                newMenuItem.DropDownItems.Add(schemaItem);
            }

            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                newMenuItem,
                openMenuItem,
                openInNewTabMenuItem,
                recentFilesMenuItem,
                saveMenuItem,
                saveAsMenuItem,
                newTab,
                new ToolStripSeparator(),
            });

            menuStrip.Items.Add(fileMenu);
            layoutPanel.Controls.Add(menuStrip, 0, 0);
            this.MainMenuStrip = menuStrip;

            // Create a container panel for tabs + button
            Panel tabPanel = new Panel();
            tabPanel.Height = 0;
            tabPanel.Dock = DockStyle.Fill;
            tabPanel.BackColor = Color.Black; // Match UI theme
            layoutPanel.Controls.Add(tabPanel, 0, 0);

            // Create TabControl
            fileTabControl = new TabControl();
            fileTabControl.Dock = DockStyle.Fill;
            fileTabControl.Name = "fileTabControl";
            fileTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            fileTabControl.ItemSize = new Size(125, 25);
            fileTabControl.SizeMode = TabSizeMode.Normal;
            fileTabControl.Appearance = TabAppearance.Normal;

            // Attach event handlers
            fileTabControl.Selecting += FileTabControl_Selecting;
            fileTabControl.DrawItem += FileTabControl_DrawItem;

            // Create Context Menu
            ContextMenuStrip tabContextMenu = new ContextMenuStrip();
            tabContextMenu.Items.Add("Close", null, CloseTab_Click);
            tabContextMenu.Items.Add("Close All", null, CloseAllTabs_Click);

            // Attach event to detect right-click
            fileTabControl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    for (int i = 0; i < fileTabControl.TabPages.Count; i++)
                    {
                        Rectangle tabRect = fileTabControl.GetTabRect(i);
                        if (tabRect.Contains(e.Location))
                        {
                            fileTabControl.SelectedIndex = i; // Select the tab that was right-clicked
                            tabContextMenu.Show(fileTabControl, e.Location); // Show the context menu
                            return;
                        }
                    }
                }
            };

            // Add TabControl inside the panel
            tabPanel.Controls.Add(fileTabControl);

            // Finally, add the TabControl to the main layout
            layoutPanel.Controls.Add(tabPanel, 0, 1);

            menuStrip.RenderMode = ToolStripRenderMode.Professional;
            menuStrip.BackColor = Color.FromArgb(30, 30, 30);
            layoutPanel.Margin = Padding.Empty;
            layoutPanel.Padding = Padding.Empty;
            layoutPanel.BackColor = Color.FromArgb(30, 30, 30);
            layoutPanel.Controls.Add(fileTabControl, 0, 1);

            Theme.ApplyTheme(this);
            menuStrip.Renderer = new DarkMenuRenderer();
            menuStrip.BackColor = Color.FromArgb(45, 45, 48);
            menuStrip.ForeColor = Color.Black;
        }
        private void CopySelectedNode()
        {
            TreeView treeView = fileTabControl.SelectedTab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;
            if (treeView?.SelectedNode?.Tag is string path)
            {
                JToken token = GetTokenAtPath(path);
                if (token != null)
                {
                    Clipboard.SetText(token.ToString(Formatting.None)); // Copy raw JSON
                }
            }
        }

        private void PasteIntoSelectedNode()
        {
            TreeView treeView = fileTabControl.SelectedTab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;
            if (treeView?.SelectedNode?.Tag is string path && Clipboard.ContainsText())
            {
                string json = Clipboard.GetText();

                try
                {
                    JToken newToken = JToken.Parse(json);
                    JToken target = GetTokenAtPath(path);

                    if (target is JObject obj && newToken is JProperty prop)
                    {
                        if (!obj.ContainsKey(prop.Name))
                        {
                            obj.Add(prop);
                        }
                    }
                    else if (target is JArray arr)
                    {
                        arr.Add(newToken);
                    }
                    else if (target?.Parent is JArray parentArr)
                    {
                        parentArr.Add(newToken);
                    }

                    UpdateTreeView();
                    UpdateRawJsonDisplay();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Invalid JSON in clipboard: " + ex.Message);
                }
            }
        }

        private void CreateNewFileFromSchema(string schemaName)
        {
            string fileName = PromptForFileName();
            if (string.IsNullOrWhiteSpace(fileName)) return;

            JObject newJson = BuildJsonFromPreset(schemaName);

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName + ".json");

            try
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(newJson, Formatting.Indented));
                CreateNewEditorTab(fileName, newJson, filePath);
                OpenFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenFileInNewTabMenu(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.Title = "Open JSON in New Tab";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OpenFileInNewTab(openFileDialog.FileName);
                }
            }
        }

        private void CloseTab_Click(object sender, EventArgs e)
        {
            if (fileTabControl.SelectedTab != null)
            {
                fileTabControl.TabPages.Remove(fileTabControl.SelectedTab);
            }
        }

        private void CloseAllTabs_Click(object sender, EventArgs e)
        {
            fileTabControl.TabPages.Clear();
            CreateNewEditorTab("Untitled", null, null);
        }

        private void FileTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            Graphics g = e.Graphics;
            Rectangle tabRect = tabControl.GetTabRect(e.Index);

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // VS-like colors
            Color tabBack = isSelected ? Color.FromArgb(50, 50, 50) : Color.FromArgb(30, 30, 30);
            Color textColor = isSelected ? Color.White : Color.Gray;
            Color borderColor = Color.FromArgb(102, 51, 153); // VS Purple
            Font tabFont = new Font("Segoe UI", 8, FontStyle.Regular);

            // Fill the tab background
            using (Brush backBrush = new SolidBrush(tabBack))
                g.FillRectangle(backBrush, tabRect);

            // Draw tab text
            string tabText = tabControl.TabPages[e.Index].Text;
            TextRenderer.DrawText(g, tabText, tabFont, tabRect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Draw VS purple border only on selected tab
            if (isSelected)
            {
                using (Pen borderPen = new Pen(borderColor, 2))
                {
                    g.DrawRectangle(borderPen, tabRect.X, tabRect.Y, tabRect.Width - 1, tabRect.Height - 1);
                }
            }
        }

        private void FileTabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (fileTabControl.SelectedTab?.Tag is EditorContext ctx)
            {
                jsonData = ctx.Json;
                currentFilePath = ctx.FilePath;
                selectedProperty = null;
                selectedPropertyPath = null;

                UpdateTreeView();
                UpdateRawJsonDisplay();
            }
        }

        private void NewTab(object sender, EventArgs e)
        {
            CreateNewEditorTab("Untitled", new JObject());
        }

        private TabPage CreateNewEditorTab(string title, JObject json, string filePath = null)
        {
            TabPage tabPage = new TabPage(title);

            SplitContainer editorUI = SetupEditor();

            FlowLayoutPanel animationPanel = editorUI.Panel2.Controls
                .OfType<FlowLayoutPanel>()
                .FirstOrDefault(f => f.Name == "animationPanel");

            tabPage.Tag = new EditorContext
            {
                Json = json ?? new JObject(),
                FilePath = filePath,
                EditorUI = editorUI,
                AnimationPanel = animationPanel
            };

            tabPage.Controls.Add(editorUI);

            TabControl tabControl = this.Controls.Find("fileTabControl", true)[0] as TabControl;
            tabControl.TabPages.Insert(tabControl.TabPages.Count, tabPage);
            tabControl.SelectedTab = tabPage;
            Theme.ApplyTheme(tabControl);

            return tabPage;
        }

        private SplitContainer SetupEditor()
        {
            // Create SplitContainer inside the layout panel
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;

            // Create Properties Panel (Left Panel)
            Panel propertiesPanel = new Panel();
            propertiesPanel.Dock = DockStyle.Fill;
            splitContainer.Panel1.Controls.Add(propertiesPanel);
            splitContainer.BorderStyle = BorderStyle.None;

            // Create a Panel to Wrap the Label and TreeView
            Panel treeContainerPanel = new Panel();
            treeContainerPanel.Dock = DockStyle.Fill; // Ensure it expands fully
            treeContainerPanel.Padding = new Padding(10,0,0,20);
            propertiesPanel.Controls.Add(treeContainerPanel);

            // JSON Properties Label
            FlowLayoutPanel headerPanel = new FlowLayoutPanel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.AutoSize = true;
            headerPanel.FlowDirection = FlowDirection.LeftToRight;
            propertiesPanel.Controls.Add(headerPanel);

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
            buttonPanel.Height = 50;
            buttonPanel.Name = "buttonPanel";
            buttonPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonPanel.Padding = new Padding(10,0,0,0);
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
            editorPanel.Dock = DockStyle.None;
            editorPanel.AutoSize = true;
            editorPanel.Width = 500;
            editorPanel.Padding = new Padding(10);

            splitContainer.Panel2.Controls.Add(editorPanel);

            Label propertyNameLabel = new Label();
            propertyNameLabel.Text = "Property Name:";
            propertyNameLabel.Location = new System.Drawing.Point(10, 40);
            editorPanel.Controls.Add(propertyNameLabel);

            TextBox propertyNameTextBox = new TextBox();
            propertyNameTextBox.Name = "propertyNameTextBox";
            propertyNameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left; // Prevents auto-stretching
            propertyNameTextBox.Location = new System.Drawing.Point(110, 40);
            propertyNameTextBox.Size = new System.Drawing.Size(200, 20); // Manually sets width
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
            propertyTypeComboBox.Size = new System.Drawing.Size(100, 20);
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
            rawJsonTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rawJsonTextBox.Location = new System.Drawing.Point(10, 280);
            rawJsonTextBox.Size = new System.Drawing.Size(450, 260);
            rawJsonTextBox.Multiline = true;
            rawJsonTextBox.ScrollBars = ScrollBars.Vertical;
            rawJsonTextBox.ReadOnly = true;
            editorPanel.Controls.Add(rawJsonTextBox);
            rawJsonTextBox.BackColor = Theme.Background;
            rawJsonTextBox.ForeColor = Theme.Foreground;

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
            presetSelector.Items.AddRange(JsonSchemas.Schemas.ToArray());
            presetSelector.SelectedIndexChanged += PresetSelector_SelectedIndexChanged;
            presetSelector.SelectedItem = "Item";
            headerPanel.Controls.Add(presetSelector);

            // Image & Animation Panels
            Panel imagePanel = new Panel();
            imagePanel.Dock = DockStyle.Left;
            imagePanel.Width = 300;
            imagePanel.Padding = new Padding(0, 0, 100, 0);
            editorPanel.Controls.Add(imagePanel);

            FlowLayoutPanel animationPanel = new FlowLayoutPanel();
            animationPanel.Dock = DockStyle.Right;
            animationPanel.Width = 300;
            animationPanel.FlowDirection = FlowDirection.TopDown;
            animationPanel.AutoScroll = true;
            animationPanel.Name = "animationPanel";
            animationPanel.WrapContents = false;
            animationPanel.Padding = new Padding(0,30,40,0);
            splitContainer.Panel2.Controls.Add(animationPanel);

            nameTextBox = new TextBox();
            nameTextBox.Name = "propertyNameTextBox";

            valueTextBox = new TextBox();
            valueTextBox.Name = "propertyValueTextBox";

            // Add tooltips
            toolTip.SetToolTip(enforcePresetCheckbox, "Whether you can edit the JSON files freely, unbound by Earthward data schemas");
            toolTip.SetToolTip(presetSelector, "The type of data you want to make. This defines what properties you can and can't add");
  
            // Update recent files
            UpdateRecentFilesMenu();

            return splitContainer;
        }

        private void OpenFileInNewTab(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            // Create a new tab with an empty JSON object for now
            TabPage newTab = CreateNewEditorTab(Path.GetFileName(filePath), new JObject(), filePath);

            // Get the editor context of the newly created tab
            EditorContext context = newTab.Tag as EditorContext;
            if (context == null) return;

            // Load the JSON and images into the new tab's context
            OpenFile(filePath);
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
                        OpenFileInNewTab(filePath);
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
        private void propertiesTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                TreeView treeView = fileTabControl.SelectedTab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;

                if (treeView.SelectedNode != null)
                {
                    // Assume each node's Tag holds a JProperty representing the JSON property.
                    if (treeView.SelectedNode.Tag is JProperty jProp)
                    {
                        // Remove the property from its parent JSON object.
                        jProp.Remove();
                    }

                    // Refresh the TreeView and raw JSON display.
                    UpdateTreeView();
                    UpdateRawJsonDisplay();
                }
            }
        }
        private void UpdateTreeView()
        {
            MarkTabAsDirty();
            if (fileTabControl.SelectedTab == null) return;

            // Find the TreeView inside the selected tab's controls
            TreeView treeView = fileTabControl.SelectedTab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;
            if (treeView == null) return; // Ensure a TreeView exists in the tab

            expandedPaths.Clear();
            SaveExpandedNodes(treeView.Nodes);

            // 🔽 Save the selected path
            if (treeView.SelectedNode != null)
                selectedPath = treeView.SelectedNode.Tag as string;
            else
                selectedPath = null;
            treeView.BeginUpdate();

            treeView.Nodes.Clear();
            PopulateTreeView(treeView.Nodes, jsonData);
            RestoreExpandedNodes(treeView.Nodes);

            // 🔽 Restore selected node
            if (!string.IsNullOrEmpty(selectedPath))
            {
                TreeNode nodeToSelect = FindNodeByPath(treeView.Nodes, selectedPath);
                if (nodeToSelect != null)
                {
                    treeView.SelectedNode = nodeToSelect;
                    treeView.SelectedNode.EnsureVisible(); // Optional: scrolls to it
                }
            }
            treeView.EndUpdate();
        }

        private TreeNode FindNodeByPath(TreeNodeCollection nodes, string path)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag as string == path)
                    return node;

                TreeNode found = FindNodeByPath(node.Nodes, path);
                if (found != null)
                    return found;
            }
            return null;
        }
        private void PropertiesTreeView_DragDrop(object sender, DragEventArgs e)
        {
            TreeView tree = sender as TreeView;
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
            Point pt = tree.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = tree.GetNodeAt(pt);

            if (draggedNode == null) return;

            string draggedPath = draggedNode.Tag?.ToString();
            string targetPath = targetNode?.Tag?.ToString();
            if (string.IsNullOrEmpty(draggedPath)) return;

            JToken draggedToken = GetTokenAtPath(draggedPath);
            if (draggedToken == null || draggedToken.Parent is JArray || draggedToken.Parent is JObject) return;

            JToken targetToken = targetPath != null ? GetTokenAtPath(targetPath) : null;

            // Remove from current parent first
            if (draggedToken.Parent is JProperty prop)
                prop.Remove();
            else if (draggedToken.Parent is JArray arr)
                arr.Remove(draggedToken);

            bool added = false;

            // ✅ Drop onto object
            if (targetToken is JObject targetObj && draggedToken is JProperty draggedProp)
            {
                targetObj.Add(draggedProp);
                added = true;
            }
            else if (targetToken is JObject targetObj2 && draggedToken.Parent is JProperty dp)
            {
                targetObj2.Add(new JProperty(dp.Name, draggedToken));
                added = true;
            }
            // ✅ Drop onto array
            else if (targetToken is JArray targetArray)
            {
                targetArray.Add(draggedToken);
                added = true;
            }
            else if (!added && jsonData is JObject rootObj && draggedToken.Parent != rootObj)
            {
                // Get the name from the dragged token
                string originalKey = draggedToken is JProperty dp2
                    ? dp2.Name
                    : draggedNode.Text.Split(':')[0].Trim(); // fallback for array values

                string key = originalKey;
                int i = 1;
                while (rootObj.ContainsKey(key))
                    key = $"{originalKey}_{i++}";

                var newProp = new JProperty(key, draggedToken is JProperty p ? p.Value : draggedToken);

                // Determine insertion point in root
                if (targetNode != null && targetNode.Parent == null)
                {
                    string targetKey = targetNode.Text.Split(':')[0].Trim();
                    var props = rootObj.Properties().ToList();
                    int insertIndex = props.FindIndex(p => p.Name == targetKey);

                    if (insertIndex != -1)
                    {
                        props.Insert(insertIndex, newProp);
                        rootObj.RemoveAll();
                        foreach (var prop2 in props)
                            rootObj.Add(prop2);
                        added = true;
                    }
                }

                // If not inserted into order, just add at end
                if (!added)
                {
                    rootObj.Add(newProp);
                    added = true;
                }
            }



            UpdateRawJsonDisplay();
            UpdateTreeView();
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
                return value.Type == JTokenType.String ? $"\"{value.Value}\"" : value.Value?.ToString() ?? "null";
            }
            return ""; // ❌ Do NOT modify object names!
        }

        private void NameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (selectedProperty == null || selectedProperty.Parent is not JObject parentObject)
                return;

            TextBox nameTextBox = sender as TextBox;
            string newName = nameTextBox.Text.Trim();

            // Prevent renaming array elements like [0]
            if (newName.StartsWith("[") && newName.EndsWith("]"))
                return;

            // Prevent duplicate keys
            if (parentObject.Property(newName) != null && parentObject.Property(newName) != selectedProperty)
                return;

            // 🔹 Save the old path before renaming
            string oldPath = selectedPropertyPath;

            // 🔹 Rename in JSON
            JProperty newProperty = new JProperty(newName, selectedProperty.Value);
            parentObject.Remove(selectedProperty.Name);
            parentObject.Add(newProperty);
            selectedProperty = newProperty;

            // 🔹 Update selectedPropertyPath with new name
            selectedPropertyPath = BuildNewPath(oldPath, newName);

            TreeView treeView = fileTabControl.SelectedTab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;
            TreeNode node = treeView?.SelectedNode;

            if (node != null)
            {
                string valueText = GetDisplayValueForTreeNode(selectedProperty.Value);

                node.Text = !string.IsNullOrEmpty(valueText) ? $"{newName}: {valueText}" : newName;
                node.Tag = selectedPropertyPath;

                UpdateChildTags(node, oldPath, selectedPropertyPath);
            }

            MarkTabAsDirty();
            UpdateRawJsonDisplay();
        }

        private string BuildNewPath(string oldPath, string newName)
        {
            int lastDot = oldPath.LastIndexOf('.');
            return lastDot >= 0 ? oldPath.Substring(0, lastDot + 1) + newName : newName;
        }

        private void PropertiesTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            string path = e.Node.Tag?.ToString();
            JToken token = GetTokenAtPath(path);
            selectedPropertyPath = path;

            if (token != null)
            {
                selectedProperty = token.Parent is JProperty prop ? prop : null;

                // ✅ Find controls inside the correct tab
                Control[] foundControls = fileTabControl.SelectedTab.Controls.Find("propertyNameTextBox", true);
                if (foundControls.Length == 0) return;
                TextBox nameTextBox = foundControls[0] as TextBox;

                foundControls = fileTabControl.SelectedTab.Controls.Find("propertyValueTextBox", true);
                Control oldValueControl = foundControls.FirstOrDefault()
                                          ?? fileTabControl.SelectedTab.Controls.Find("propertyValueCheckBox", true).FirstOrDefault();

                foundControls = fileTabControl.SelectedTab.Controls.Find("propertyTypeComboBox", true);
                if (foundControls.Length == 0) return;
                ComboBox typeComboBox = foundControls[0] as ComboBox;

                Panel editorPanel = nameTextBox.Parent as Panel;

                // ✅ Handle array elements correctly
                string propertyName = e.Node.Text.Contains(":") ? e.Node.Text.Split(':')[0].Trim() : e.Node.Text;
                nameTextBox.Text = propertyName;
                nameTextBox.ReadOnly = path.Contains("[") && path.Contains("]"); // Disable editing for array indices

                nameTextBox.TextChanged -= NameTextBox_TextChanged;
                nameTextBox.TextChanged += NameTextBox_TextChanged;

                // Remove old value field if it exists
                if (oldValueControl != null)
                {
                    editorPanel.Controls.Remove(oldValueControl);
                }

                Control newControl = null; // Holds the new control

                typeComboBox.SelectedItem = token.Type switch
                {
                    JTokenType.String => "String",
                    JTokenType.Integer or JTokenType.Float => "Number",
                    JTokenType.Boolean => "Boolean",
                    JTokenType.Null => "Null",
                    JTokenType.Object => "Object",
                    JTokenType.Array => "Array",
                    _ => "Unknown"
                };

                if (token is JValue jValue)
                {
                    string selectedType = jValue.Type switch
                    {
                        JTokenType.String => "String",
                        JTokenType.Integer or JTokenType.Float => "Number",
                        JTokenType.Boolean => "Boolean",
                        JTokenType.Null => "Null",
                        JTokenType.Object => "Object",
                        JTokenType.Array => "Array",
                        _ => "Unknown"
                    };

                    if (selectedType == "Boolean")
                    {
                        // ✅ Use a CheckBox for booleans
                        CheckBox boolCheckBox = new CheckBox
                        {
                            Name = "propertyValueCheckBox",
                            Checked = jValue.Value<bool>(),
                            Location = new Point(110, 72),
                            AutoSize = true
                        };
                        boolCheckBox.CheckedChanged += (s, ev) => UpdatePropertyFromControl(boolCheckBox.Checked);
                        newControl = boolCheckBox;
                    }
                    else if (selectedType == "Number")
                    {
                        // ✅ Use NumericUpDown for numbers
                        NumericUpDown numberBox = new NumericUpDown
                        {
                            Name = "propertyValueCheckBox",
                            Location = new Point(110, 70),
                            Size = new Size(300, 20),
                            DecimalPlaces = token.Type == JTokenType.Float ? 2 : 0,
                            Minimum = decimal.MinValue,
                            Maximum = decimal.MaxValue,
                            Value = Convert.ToDecimal(jValue.Value)
                        };

                        numberBox.ValueChanged += (s, ev) =>
                        {
                            UpdatePropertyFromControl( numberBox.Value);
                        };

                        newControl = numberBox;
                    }
                    else if (selectedType == "String" && TryGetEnumOptionsForProperty(propertyName, out var enumOptions))
                    {
                        ComboBox comboBox = new ComboBox
                        {
                            Name = "propertyValueComboBox",
                            Location = new Point(110, 70),
                            Size = new Size(300, 20),
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };

                        comboBox.Items.AddRange(enumOptions);
                        comboBox.SelectedItem = jValue.Value?.ToString() ?? enumOptions.FirstOrDefault();

                        comboBox.SelectedIndexChanged += (s, ev) =>
                        {
                            UpdatePropertyFromControl(comboBox.SelectedItem?.ToString());
                        };

                        newControl = comboBox;
                    }
                    else
                    {
                        // ✅ Use a TextBox for strings & everything else
                        TextBox valueTextBox = new TextBox
                        {
                            Name = "propertyValueTextBox",
                            Location = new Point(110, 70),
                            Size = new Size(300, 20),
                            Text = jValue.Value?.ToString() ?? "",
                            Height = 100,
                            Multiline = true
                        };
                        valueTextBox.TextChanged += (s, ev) => UpdatePropertyFromControl(valueTextBox.Text);
                        newControl = valueTextBox;
                    }
                }

                // ✅ Add new control if needed
                if (newControl != null)
                {
                    editorPanel.Controls.Add(newControl);
                    newControl.BringToFront();
                }
            }
        }

        private bool TryGetEnumOptionsForProperty(string propertyName, out string[] enumOptions)
        {
            enumOptions = null;

            if (presetSelector?.SelectedItem is string presetName &&
                JsonSchemas.Properties.TryGetValue(presetName, out var fields))
            {
                var field = fields.FirstOrDefault(f => f.Name == propertyName);
                if (field.EnumOptions != null && field.EnumOptions.Length > 0)
                {
                    enumOptions = field.EnumOptions;
                    return true;
                }
            }

            return false;
        }

        private void UpdatePropertyFromControl(object newValue)
        {
            if (string.IsNullOrEmpty(selectedPropertyPath)) return;

            JToken token = GetTokenAtPath(selectedPropertyPath);
            if (token == null) return;

            // Create new JValue from input
            JValue newJValue = newValue switch
            {
                bool b => new JValue(b),
                int or float or double or decimal => new JValue(Convert.ToDouble(newValue)),
                _ => new JValue(newValue?.ToString())
            };

            if (selectedProperty != null)
            {
                selectedProperty.Value = newJValue;
            }
            else if (token.Parent is JArray arr)
            {
                int index = arr.IndexOf(token);
                if (index != -1)
                {
                    arr[index] = newJValue;
                }
            }

            TreeView treeView = fileTabControl.SelectedTab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;
            TreeNode node = treeView?.SelectedNode;

            if (node != null)
            {
                string propertyName;
                string valueText;

                if (selectedProperty != null)
                {
                    propertyName = selectedProperty.Name;
                    valueText = GetDisplayValueForTreeNode(selectedProperty.Value);
                }
                else
                {
                    propertyName = node.Text.Split(':')[0].Trim(); // e.g. "[2]"
                    valueText = GetDisplayValueForTreeNode(newJValue);
                }

                node.Text = !string.IsNullOrEmpty(valueText) ? $"{propertyName}: {valueText}" : propertyName;

            }
            MarkTabAsDirty();
            UpdateRawJsonDisplay();
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
            MarkTabAsDirty();

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
            if (fileTabControl.SelectedTab == null) return;

            // Find controls inside the selected tab only
            var tab = fileTabControl.SelectedTab;

            TreeView treeView = tab.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;
            ComboBox presetSelector = tab.Controls.Find("presetSelector", true).FirstOrDefault() as ComboBox;
            CheckBox enforcePresetCheckbox = tab.Controls.Find("enforcePresetCheckbox", true).FirstOrDefault() as CheckBox;

            if (treeView == null || presetSelector == null || enforcePresetCheckbox == null)
                return;

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
                if (JsonSchemas.Properties.TryGetValue(selectedPreset, out var fields))
                {
                    var available = fields
                        .Where(f => !parentObj.ContainsKey(f.Name))
                        .Select(f => f.Name)
                        .ToList();

                    if (available.Count == 0)
                    {
                        MessageBox.Show("All allowed properties already exist.", "Nothing To Add", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }


                    newPropertyName = Prompt.ShowDropdown("Add Property", $"Add a variable to your Earthward {presetSelector.SelectedItem}", available);
                    if (string.IsNullOrEmpty(newPropertyName)) return;

                    // Add default value from schema
                    var field = fields.First(f => f.Name == newPropertyName);
                    parentObj[newPropertyName] = JToken.FromObject(field.DefaultValue ?? "");
                }
            }
            else 
            {
                // Add a generic "NewProperty" with incremental suffix
                int counter = 1;
                newPropertyName = "Name";

                while (parentObj.ContainsKey(newPropertyName))
                    newPropertyName = $"Name{counter++}";

                ComboBox typeComboBox = tab.Controls.Find("propertyTypeComboBox", true).FirstOrDefault() as ComboBox;
                string selectedType = typeComboBox?.SelectedItem?.ToString();

                JToken defaultValue = selectedType switch
                {
                    "String" => new JValue(""),
                    "Number" => new JValue(0),
                    "Boolean" => new JValue(false),
                    "Null" => JValue.CreateNull(),
                    "Object" => new JObject(),
                    "Array" => new JArray(),
                    _ => new JValue("")
                };

                var newProp = new JProperty(newPropertyName, defaultValue);
                parentObj.Add(newProp);
                selectedProperty = newProp; // ✅ set selected property

                string fullPath = string.IsNullOrEmpty(parentPath)
                    ? newPropertyName
                    : $"{parentPath}.{newPropertyName}";

                SelectNodeByPath(fullPath);

                UpdateRawJsonDisplay();
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
            if (fileTabControl.SelectedTab == null) return;

            // Find the Raw JSON TextBox inside the selected tab
            TextBox rawJsonTextBox = fileTabControl.SelectedTab.Controls.Find("rawJsonTextBox", true).FirstOrDefault() as TextBox;
            if (rawJsonTextBox == null) return; // Ensure the textbox exists

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 4, // 👈 Set indentation to 4 spaces
                IndentChar = ' '
            })
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, jsonData);
                rawJsonTextBox.Text = stringWriter.ToString();
            }

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

        private void UpdateChildTags(TreeNode parentNode, string oldPath, string newPath)
        {
            foreach (TreeNode child in parentNode.Nodes)
            {
                if (child.Tag is string childPath && childPath.StartsWith(oldPath))
                {
                    string updatedPath = newPath + childPath.Substring(oldPath.Length);
                    child.Tag = updatedPath;

                    if (child.Nodes.Count > 0)
                    {
                        UpdateChildTags(child, childPath, updatedPath); // recursive update
                    }
                }
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

        private JObject BuildJsonFromPreset(string presetName)
        {
            JObject result = new JObject();

            if (JsonSchemas.Presets.TryGetValue(presetName, out var fields))
            {
                foreach (var field in fields)
                {
                    JToken value = field.type switch
                    {
                        "String" => new JValue(field.defaultValue?.ToString() ?? ""),
                        "Number" => new JValue(Convert.ToDouble(field.defaultValue ?? 0)),
                        "Boolean" => new JValue(Convert.ToBoolean(field.defaultValue ?? false)),
                        "Object" => field.defaultValue is JObject jo ? jo.DeepClone() : new JObject(),
                        "Array" => field.defaultValue is JArray ja ? ja.DeepClone() : new JArray(),
                        "Null" => JValue.CreateNull(),
                        _ => new JValue(field.defaultValue?.ToString() ?? "")
                    };

                    result[field.name] = value;
                }
            }

            return result;
        }

        private void NewFile(object sender, EventArgs e)
        {
            // 🚀 Ask the user for a file name
            string fileName = PromptForFileName();
            if (string.IsNullOrWhiteSpace(fileName)) return; // If canceled, do nothing

            // 🚀 Create an empty JSON object
            JObject newJson = BuildJsonFromPreset("Empty");

            // 🚀 Get the file path (Default to Documents folder)
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName + ".json");

            try
            {
                // 🚀 Save an empty JSON file
                File.WriteAllText(filePath, JsonConvert.SerializeObject(newJson, Formatting.Indented));

                // 🚀 Open a new tab with the newly created JSON file
                CreateNewEditorTab(fileName, newJson, filePath);
                OpenFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private string PromptForFileName()
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 300;
                prompt.Height = 150;
                prompt.Text = "Enter File Name";

                Label textLabel = new Label() { Left = 20, Top = 20, Text = "File Name:" };
                TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 240 };
                Button confirmation = new Button() { Text = "OK", Left = 160, Width = 100, Top = 80 };

                confirmation.DialogResult = DialogResult.OK;
                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;
                prompt.StartPosition = FormStartPosition.CenterParent;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
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
                JObject jsonData = JObject.Parse(fileContent);

                // Get current tab
                TabPage selectedTab = fileTabControl.SelectedTab;

                if (selectedTab == null || selectedTab.Text == "+")
                    return; // Don't allow opening into the + tab

                selectedTab.Text = Path.GetFileName(filePath); // Update tab title

                // Get or create the editor context from the tab
                EditorContext context = selectedTab.Tag as EditorContext;
                if (context == null)
                {
                    context = new EditorContext();
                    selectedTab.Tag = context;
                }

                // Make sure the tab has an Editor UI
                if (context.EditorUI == null)
                {
                    context.EditorUI = SetupEditor();
                    selectedTab.Controls.Clear();
                    selectedTab.Controls.Add(context.EditorUI);
                }

                context.Json = jsonData;
                context.FilePath = filePath;
                this.jsonData = jsonData;
                currentFilePath = filePath;

                // Update UI labels
                RecentFiles.Add(filePath);

                LoadImagesForFile(context.AnimationPanel, filePath);
                UpdateRecentFilesMenu();
                UpdateTreeView();
                UpdateRawJsonDisplay();
                MarkTabAsSaved();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadImagesForFile(FlowLayoutPanel animationPanel, string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || animationPanel == null) return;

            // Clear old images for this tab
            animationPanel.Controls.Clear();

            string jsonDirectory = Path.GetDirectoryName(filePath);
            string jsonFileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            string baseImagePath = Path.Combine(jsonDirectory, jsonFileNameWithoutExt + ".png");
            string[] extraImagePaths = Directory.GetFiles(jsonDirectory, jsonFileNameWithoutExt + "_*.png");

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

                    animationPanel.Controls.Add(picBox); // 🔥 Ensures image is added to the right panel

                    var animation = new SpriteAnimation(picBox, imagePath);
                    animationLayers.Add(animation);
                }
            }
        }

        private void MarkTabAsDirty()
        {
            var tab = fileTabControl.SelectedTab;
            if (tab != null && !tab.Text.EndsWith("*"))
            {
                tab.Text += " *";
            }
        }
        private void MarkTabAsSaved()
        {
            var tab = fileTabControl.SelectedTab;

            if (tab != null)
            {
                string fileName = Path.GetFileName(currentFilePath);

                if (!string.IsNullOrEmpty(fileName))
                {
                    tab.Text = fileName;
                }
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
                    MarkTabAsSaved();
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
                    MarkTabAsSaved();
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
            else if (keyData == Keys.Delete)
            {
                // Find the active TreeView in the selected tab
                TreeView treeView = fileTabControl.SelectedTab?.Controls.Find("propertiesTreeView", true).FirstOrDefault() as TreeView;

                if (treeView?.SelectedNode != null)
                {
                    string path = treeView.SelectedNode.Tag?.ToString();
                    JToken token = GetTokenAtPath(path);

                    if (token != null)
                    {
                        if (token.Parent is JProperty prop)
                        {
                            prop.Remove();
                        }
                        else if (token.Parent is JArray array)
                        {
                            int index = array.IndexOf(token);
                            if (index >= 0)
                                array.RemoveAt(index);
                        }

                        UpdateRawJsonDisplay();
                        UpdateTreeView();
                    }
                }

                return true; // Mark Delete as handled
            }
            else if (keyData == (Keys.Control | Keys.C))
            {
                CopySelectedNode();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.V))
            {
                PasteIntoSelectedNode();
                return true;
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