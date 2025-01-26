using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetInventory
{
    internal sealed class TreeViewItem<T> : TreeViewItem where T : TreeElement
    {
        public T Data { get; }

        public TreeViewItem(int id, int depth, string displayName, T data) : base(id, depth, displayName)
        {
            Data = data;
        }
    }

    internal class TreeViewWithTreeModel<T> : TreeView where T : TreeElement
    {
        private readonly List<TreeViewItem> _rows = new List<TreeViewItem>(100);
        public event Action TreeChanged;

        protected TreeModel<T> TreeModel { get; private set; }

        public event Action<IList<TreeViewItem>> BeforeDroppingDraggedItems;
        public event Action<IList<int>> OnSelectionChanged;
        public event Action<int> OnDoubleClickedItem;

        public TreeViewWithTreeModel(TreeViewState state, TreeModel<T> model) : base(state)
        {
            Init(model);
        }

        public TreeViewWithTreeModel(TreeViewState state, MultiColumnHeader multiColumnHeader, TreeModel<T> model) : base(state, multiColumnHeader)
        {
            Init(model);
        }

        private void Init(TreeModel<T> model)
        {
            TreeModel = model;
            TreeModel.ModelChanged += ModelChanged;
        }

        private void ModelChanged()
        {
            if (TreeChanged != null) TreeChanged();

            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem<T>(TreeModel.Root.TreeId, -1, TreeModel.Root.TreeName, TreeModel.Root);
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (TreeModel.Root == null)
            {
                Debug.LogError("tree model root is null. did you call SetData()?");
            }

            _rows.Clear();
            if (!string.IsNullOrEmpty(searchString))
            {
                Search(TreeModel.Root, searchString, _rows);
            }
            else
            {
                if (TreeModel.Root.HasChildren) AddChildrenRecursive(TreeModel.Root, 0, _rows);
            }

            // We still need to set up the child parent information for the rows since this 
            // information is used by the TreeView internal logic (navigation, dragging etc.)
            SetupParentsAndChildrenFromDepths(root, _rows);

            return _rows;
        }

        private void AddChildrenRecursive(T parent, int depth, IList<TreeViewItem> newRows)
        {
            foreach (T child in parent.Children)
            {
                TreeViewItem<T> item = new TreeViewItem<T>(child.TreeId, depth, child.TreeName, child);
                newRows.Add(item);

                if (child.HasChildren)
                {
                    if (IsExpanded(child.TreeId))
                    {
                        AddChildrenRecursive(child, depth + 1, newRows);
                    }
                    else
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        private void Search(T searchFromThis, string search, List<TreeViewItem> result)
        {
            if (string.IsNullOrEmpty(search)) throw new ArgumentException("Invalid search: cannot be null or empty", "search");

            const int kItemDepth = 0; // tree is flattened when searching

            Stack<T> stack = new Stack<T>();
            if (searchFromThis.Children != null)
            {
                foreach (TreeElement element in searchFromThis.Children)
                {
                    stack.Push((T)element);
                }
            }
            while (stack.Count > 0)
            {
                T current = stack.Pop();
                // Matches search?
                if (current.TreeName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(new TreeViewItem<T>(current.TreeId, kItemDepth, current.TreeName, current));
                }

                if (current.Children != null && current.Children.Count > 0)
                {
                    foreach (TreeElement element in current.Children)
                    {
                        stack.Push((T)element);
                    }
                }
            }
            SortSearchResult(result);
        }

        protected virtual void SortSearchResult(List<TreeViewItem> rows)
        {
            rows.Sort((x, y) => EditorUtility.NaturalCompare(x.displayName, y.displayName)); // sort by displayName by default, can be overriden for multicolumn solutions
        }

        protected override IList<int> GetAncestors(int id)
        {
            return TreeModel.GetAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            return TreeModel.GetDescendantsThatHaveChildren(id);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            OnSelectionChanged?.Invoke(selectedIds);
            base.SelectionChanged(selectedIds);
        }

        protected override void DoubleClickedItem(int id)
        {
            OnDoubleClickedItem?.Invoke(id);
            base.DoubleClickedItem(id);
        }

        // Dragging
        //-----------

        private const string _genericDragID = "GenericDragColumnDragging";

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (hasSearch) return;

            DragAndDrop.PrepareStartDrag();
            List<TreeViewItem> draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
            DragAndDrop.SetGenericData(_genericDragID, draggedRows);
            DragAndDrop.objectReferences = new UnityEngine.Object[] {}; // this IS required for dragging to work
            string title = draggedRows.Count == 1 ? draggedRows[0].displayName : "< Multiple >";
            DragAndDrop.StartDrag(title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // Check if we can handle the current drag data (could be dragged in from other areas/windows in the editor)
            List<TreeViewItem> draggedRows = DragAndDrop.GetGenericData(_genericDragID) as List<TreeViewItem>;
            if (draggedRows == null) return DragAndDropVisualMode.None;

            // Parent item is null when dragging outside any tree view items.
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                case DragAndDropPosition.BetweenItems:
                {
                    bool validDrag = ValidDrag(args.parentItem, draggedRows);
                    if (args.performDrop && validDrag)
                    {
                        T parentData = ((TreeViewItem<T>)args.parentItem).Data;
                        OnDropDraggedElementsAtIndex(draggedRows, parentData, args.insertAtIndex == -1 ? 0 : args.insertAtIndex);
                    }
                    return validDrag ? DragAndDropVisualMode.Move : DragAndDropVisualMode.None;
                }

                case DragAndDropPosition.OutsideItems:
                {
                    if (args.performDrop)
                        OnDropDraggedElementsAtIndex(draggedRows, TreeModel.Root, TreeModel.Root.Children.Count);

                    return DragAndDropVisualMode.Move;
                }

                default:
                    Debug.LogError("Unhandled enum " + args.dragAndDropPosition);
                    return DragAndDropVisualMode.None;
            }
        }

        protected virtual void OnDropDraggedElementsAtIndex(List<TreeViewItem> draggedRows, T parent, int insertIndex)
        {
            if (BeforeDroppingDraggedItems != null) BeforeDroppingDraggedItems(draggedRows);

            List<TreeElement> draggedElements = new List<TreeElement>();
            foreach (TreeViewItem x in draggedRows)
            {
                draggedElements.Add(((TreeViewItem<T>)x).Data);
            }

            int[] selectedIDs = draggedElements.Select(x => x.TreeId).ToArray();
            TreeModel.MoveElements(parent, insertIndex, draggedElements);
            SetSelection(selectedIDs, TreeViewSelectionOptions.RevealAndFrame);
        }

        private bool ValidDrag(TreeViewItem parent, List<TreeViewItem> draggedItems)
        {
            TreeViewItem currentParent = parent;
            while (currentParent != null)
            {
                if (draggedItems.Contains(currentParent)) return false;
                currentParent = currentParent.parent;
            }
            return false; // always set to false for now to disable weird dropping
        }
    }
}
