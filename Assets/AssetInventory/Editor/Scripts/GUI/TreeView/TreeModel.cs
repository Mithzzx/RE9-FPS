using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AssetInventory
{
    // The TreeModel is a utility class working on a list of serializable TreeElements where the order and the depth of each TreeElement define
    // the tree structure. Note that the TreeModel itself is not serializable (in Unity we are currently limited to serializing lists/arrays) but the 
    // input list is.
    // The tree representation (parent and children references) are then build internally using TreeElementUtility.ListToTree (using depth 
    // values of the elements). 
    // The first element of the input list is required to have depth == -1 (the hiddenroot) and the rest to have
    // depth >= 0 (otherwise an exception will be thrown)

    public sealed class TreeModel<T> where T : TreeElement
    {
        private IList<T> m_Data;
        private int m_MaxID;

        public T Root { get; private set; }

        public event Action ModelChanged;

        public int NumberOfDataElements => m_Data.Count;

        public TreeModel(IList<T> data)
        {
            SetData(data);
        }

        public T Find(int id)
        {
            if (_itemLookup != null)
            {
                if (_itemLookup.TryGetValue(id, out T item)) return item;
                return default(T);
            }
            return m_Data.FirstOrDefault(element => element.TreeId == id);
        }

        public IList<T> GetData() => m_Data;

        private Dictionary<int, T> _itemLookup = new Dictionary<int, T>();

        public void SetData(IList<T> data, bool sortFoldersFirst = false)
        {
            Init(data);

            if (sortFoldersFirst)
            {
                foreach (T info in data)
                {
                    if (!info.HasChildren) continue;
                    if (info.Children[0].HasChildren) continue;

                    // cater for cases where there are no subfolders, faster than scanning upfront
                    int firstId = info.Children[0].TreeId;

                    do
                    {
                        TreeElement temp = info.Children[0];
                        info.Children.RemoveAt(0);
                        info.Children.Add(temp);
                    } while (!info.Children[0].HasChildren && info.Children[0].TreeId != firstId);
                }
            }

            CacheItems();
        }

        private void CacheItems()
        {
            try
            {
                _itemLookup = m_Data.ToDictionary(x => x.TreeId, x => x);
            }
            catch (Exception)
            {
                // in case of duplicate keys disable caching
                _itemLookup = null;
            }
        }

        private void Init(IList<T> data)
        {
            if (data == null) throw new ArgumentNullException("data", "Input data is null. Ensure input is a non-null list.");

            m_Data = data;
            try
            {
                if (m_Data.Count > 0) Root = TreeElementUtility.ListToTree(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Invalid tree structure, result potentially incomplete: " + e.Message);
            }

            m_MaxID = m_Data.Count > 0 ? m_Data.Max(e => e.TreeId) : 0;
        }

        private int GenerateUniqueID()
        {
            return ++m_MaxID;
        }

        public IList<int> GetAncestors(int id)
        {
            List<int> parents = new List<int>();
            TreeElement T = Find(id);
            if (T != null)
            {
                while (T.Parent != null)
                {
                    parents.Add(T.Parent.TreeId);
                    T = T.Parent;
                }
            }
            return parents;
        }

        public IList<int> GetDescendantsThatHaveChildren(int id)
        {
            T searchFromThis = Find(id);
            if (searchFromThis != null)
            {
                return GetParentsBelowStackBased(searchFromThis);
            }
            return new List<int>();
        }

        private IList<int> GetParentsBelowStackBased(TreeElement searchFromThis)
        {
            Stack<TreeElement> stack = new Stack<TreeElement>();
            stack.Push(searchFromThis);

            List<int> parentsBelow = new List<int>();
            while (stack.Count > 0)
            {
                TreeElement current = stack.Pop();
                if (current.HasChildren)
                {
                    parentsBelow.Add(current.TreeId);
                    foreach (TreeElement T in current.Children)
                    {
                        stack.Push(T);
                    }
                }
            }

            return parentsBelow;
        }

        public void RemoveElements(IList<int> elementIDs)
        {
            IList<T> elements = m_Data.Where(element => elementIDs.Contains(element.TreeId)).ToArray();
            RemoveElements(elements);
        }

        public void RemoveElements(IList<T> elements)
        {
            foreach (T element in elements)
            {
                if (element == Root) throw new ArgumentException("It is not allowed to remove the root element");
            }

            IList<T> commonAncestors = TreeElementUtility.FindCommonAncestorsWithinList(elements);

            foreach (T element in commonAncestors)
            {
                element.Parent.Children.Remove(element);
                element.Parent = null;
            }

            TreeElementUtility.TreeToList(Root, m_Data);

            Changed();
        }

        public void AddElements(IList<T> elements, TreeElement parent, int insertPosition)
        {
            if (elements == null) throw new ArgumentNullException("elements", "elements is null");
            if (elements.Count == 0) throw new ArgumentNullException("elements", "elements Count is 0: nothing to add");
            if (parent == null) throw new ArgumentNullException("parent", "parent is null");

            if (parent.Children == null) parent.Children = new List<TreeElement>();

            parent.Children.InsertRange(insertPosition, elements);
            foreach (T element in elements)
            {
                element.Parent = parent;
                element.Depth = parent.Depth + 1;
                TreeElementUtility.UpdateDepthValues(element);
            }

            TreeElementUtility.TreeToList(Root, m_Data);

            Changed();
        }

        public void AddRoot(T root)
        {
            if (root == null) throw new ArgumentNullException("root", "root is null");
            if (m_Data == null) throw new InvalidOperationException("Internal Error: data list is null");
            if (m_Data.Count != 0) throw new InvalidOperationException("AddRoot is only allowed on empty data list");

            root.TreeId = GenerateUniqueID();
            root.Depth = -1;
            m_Data.Add(root);
        }

        public void AddElement(T element, TreeElement parent, int insertPosition)
        {
            if (element == null) throw new ArgumentNullException("element", "element is null");
            if (parent == null) throw new ArgumentNullException("parent", "parent is null");
            if (parent.Children == null) parent.Children = new List<TreeElement>();

            parent.Children.Insert(insertPosition, element);
            element.Parent = parent;

            TreeElementUtility.UpdateDepthValues(parent);
            TreeElementUtility.TreeToList(Root, m_Data);

            Changed();
        }

        public void MoveElements(TreeElement parentElement, int insertionIndex, List<TreeElement> elements)
        {
            if (insertionIndex < 0) throw new ArgumentException("Invalid input: insertionIndex is -1, client needs to decide what index elements should be reparented at");

            // Invalid reparenting input
            if (parentElement == null) return;

            // We are moving items, so we adjust the insertion index to accomodate that any items above the insertion index is removed before inserting
            if (insertionIndex > 0) insertionIndex -= parentElement.Children.GetRange(0, insertionIndex).Count(elements.Contains);

            // Remove draggedItems from their parents
            foreach (TreeElement draggedItem in elements)
            {
                draggedItem.Parent.Children.Remove(draggedItem); // remove from old parent
                draggedItem.Parent = parentElement; // set new parent
            }

            if (parentElement.Children == null) parentElement.Children = new List<TreeElement>();

            // Insert dragged items under new parent
            parentElement.Children.InsertRange(insertionIndex, elements);

            TreeElementUtility.UpdateDepthValues(Root);
            TreeElementUtility.TreeToList(Root, m_Data);

            Changed();
        }

        private void Changed()
        {
            CacheItems();
            ModelChanged?.Invoke();
        }
    }

    #region Tests

    class TreeModelTests
    {
        [Test]
        public static void TestTreeModelCanAddElements()
        {
            TreeElement root = new TreeElement {TreeName = "Root", Depth = -1};
            List<TreeElement> listOfElements = new List<TreeElement>();
            listOfElements.Add(root);

            TreeModel<TreeElement> model = new TreeModel<TreeElement>(listOfElements);
            model.AddElement(new TreeElement {TreeName = "Element"}, root, 0);
            model.AddElement(new TreeElement {TreeName = "Element " + root.Children.Count}, root, 0);
            model.AddElement(new TreeElement {TreeName = "Element " + root.Children.Count}, root, 0);
            model.AddElement(new TreeElement {TreeName = "Sub Element"}, root.Children[1], 0);

            // Assert order is correct
            string[] namesInCorrectOrder = {"Root", "Element 2", "Element 1", "Sub Element", "Element"};
            Assert.AreEqual(namesInCorrectOrder.Length, listOfElements.Count, "Result count does not match");
            for (int i = 0; i < namesInCorrectOrder.Length; ++i)
            {
                Assert.AreEqual(namesInCorrectOrder[i], listOfElements[i].TreeName);
            }

            // Assert depths are valid
            TreeElementUtility.ValidateDepthValues(listOfElements);
        }

        [Test]
        public static void TestTreeModelCanRemoveElements()
        {
            TreeElement root = new TreeElement {TreeName = "Root", Depth = -1};
            List<TreeElement> listOfElements = new List<TreeElement>();
            listOfElements.Add(root);

            TreeModel<TreeElement> model = new TreeModel<TreeElement>(listOfElements);
            model.AddElement(new TreeElement {TreeName = "Element"}, root, 0);
            model.AddElement(new TreeElement {TreeName = "Element " + root.Children.Count}, root, 0);
            model.AddElement(new TreeElement {TreeName = "Element " + root.Children.Count}, root, 0);
            model.AddElement(new TreeElement {TreeName = "Sub Element"}, root.Children[1], 0);

            model.RemoveElements(new[] {root.Children[1].Children[0], root.Children[1]});

            // Assert order is correct
            string[] namesInCorrectOrder = {"Root", "Element 2", "Element"};
            Assert.AreEqual(namesInCorrectOrder.Length, listOfElements.Count, "Result count does not match");
            for (int i = 0; i < namesInCorrectOrder.Length; ++i)
            {
                Assert.AreEqual(namesInCorrectOrder[i], listOfElements[i].TreeName);
            }

            // Assert depths are valid
            TreeElementUtility.ValidateDepthValues(listOfElements);
        }
    }

    #endregion
}
