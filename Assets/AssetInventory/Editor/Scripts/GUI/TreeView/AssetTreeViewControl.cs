using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssetInventory
{
    internal sealed class AssetTreeViewControl : TreeViewWithTreeModel<AssetInfo>
    {
        private const float ROW_HEIGHT = 20f;
        private const float TOGGLE_WIDTH = 20f;

        private enum Columns
        {
            Name,
            Tags,
            Version,
            Indexed
        }

        private readonly List<int> _previousSelection = new List<int>();

        public AssetTreeViewControl(TreeViewState state, MultiColumnHeader multiColumnHeader, TreeModel<AssetInfo> model) : base(state, multiColumnHeader, model)
        {
            rowHeight = ROW_HEIGHT * AssetInventory.Config.rowHeightMultiplier;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (ROW_HEIGHT - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = TOGGLE_WIDTH;

            Reload();
        }

        public override void OnGUI(Rect rect)
        {
            // store previous selection to support CTRL-click to toggle selection later
            _previousSelection.Clear();
            _previousSelection.AddRange(state.selectedIDs);

            base.OnGUI(rect);
        }

        protected override void SingleClickedItem(int id)
        {
            // support CTRL-click to toggle selection since tree does not natively support this
            if (Event.current.modifiers != EventModifiers.Control) return;

            if (_previousSelection.Contains(id))
            {
                state.selectedIDs.Remove(id);
                SetSelection(state.selectedIDs, TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        // only build the visible rows, the backend has the full tree information 
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            IList<TreeViewItem> rows = base.BuildRows(root);
            return rows;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            TreeViewItem<AssetInfo> item = (TreeViewItem<AssetInfo>)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (Columns)args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, TreeViewItem<AssetInfo> item, Columns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc. in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case Columns.Tags:
                    if (item.Data.PackageTags.Count > 0)
                    {
                        EditorGUI.LabelField(cellRect, string.Join(", ", item.Data.PackageTags.Select(t => t.Name)));
                    }
                    break;

                case Columns.Version:
                    bool updateAvailable = item.Data.IsUpdateAvailable((List<AssetInfo>)TreeModel.GetData());

                    Rect versionRect = cellRect;
                    versionRect.width -= 16;

                    // check if version is missing
                    if ((item.Data.AssetSource == Asset.Source.Archive || item.Data.AssetSource == Asset.Source.CustomPackage) && string.IsNullOrWhiteSpace(item.Data.GetVersion()))
                    {
                        if (AssetInventory.ShowAdvanced() && GUI.Button(versionRect, "enter manually"))
                        {
                            NameUI textUI = new NameUI();
                            textUI.Init("", newVersion => AssetInventory.SetVersion(item.Data, newVersion));
                            PopupWindow.Show(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0), textUI);
                        }
                    }
                    else
                    {
                        GUIContent version = new GUIContent(item.Data.GetVersion());
                        EditorGUI.LabelField(versionRect, version);

                        if (updateAvailable)
                        {
                            Vector2 size = EditorStyles.label.CalcSize(version);
                            Texture statusIcon = EditorGUIUtility.IconContent("Update-Available", "|Update Available").image;
                            Rect statusRect = cellRect;
                            statusRect.x += Mathf.Min(size.x, cellRect.width - 16);
                            statusRect.y += 1;
                            statusRect.width = 16;
                            statusRect.height = 16;
                            Color color = item.Data.AssetSource == Asset.Source.CustomPackage ? Color.gray : Color.white;
                            GUI.DrawTexture(statusRect, statusIcon, ScaleMode.StretchToFill, true, 0, color, Vector4.zero, Vector4.zero);
                        }
                    }
                    break;

                case Columns.Indexed:
                    if (item.Data.IsIndexed)
                    {
                        Texture indexedIcon = UIStyles.IconContent("Valid", "d_Valid", "|Indexed").image;
                        Rect indexedRect = cellRect;
                        indexedRect.x += indexedRect.width / 2 - 8;
                        indexedRect.width = 16;
                        indexedRect.height = 16;
                        GUI.DrawTexture(indexedRect, indexedIcon);
                    }
                    break;

                case Columns.Name:
                    Rect toggleRect = cellRect;
                    toggleRect.x += GetContentIndent(item);
                    toggleRect.width = TOGGLE_WIDTH - 3;
                    if (item.Data.Id > 0)
                    {
                        if (item.Data.PreviewTexture != null)
                        {
                            GUI.DrawTexture(toggleRect, item.Data.PreviewTexture);
                        }
                        else
                        {
                            Texture icon = item.Data.GetFallbackIcon();
                            if (icon != null) GUI.DrawTexture(toggleRect, icon);
                        }
                    }
                    else
                    {
                        Texture folderIcon = EditorGUIUtility.IconContent("d_Folder Icon").image;
                        GUI.DrawTexture(toggleRect, folderIcon);
                    }

                    // show default icon and label
                    args.rowRect = cellRect;
                    base.RowGUI(args);
                    break;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            MultiColumnHeaderState.Column[] columns =
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    minWidth = 60,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Tags"),
                    contextMenuText = "Tags",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 150,
                    minWidth = 30,
                    maxWidth = 300,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Version"),
                    contextMenuText = "Version",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 150,
                    minWidth = 30,
                    maxWidth = 300,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Indexed"),
                    contextMenuText = "Indexed",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 60,
                    minWidth = 30,
                    maxWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                }
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof (Columns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            MultiColumnHeaderState state = new MultiColumnHeaderState(columns);
            return state;
        }
    }
}
