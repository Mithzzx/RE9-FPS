using System;
using System.Collections.Generic;
using SQLite;
using UnityEngine;

namespace AssetInventory
{
    [Serializable]
    public class TreeElement
    {
        [SerializeField] protected int m_ID;
        [SerializeField] protected string m_Name;
        [SerializeField] protected int m_Depth;
        [NonSerialized] TreeElement _parent;
        [NonSerialized] List<TreeElement> _children;

        [Ignore]
        public int Depth
        {
            get => m_Depth;
            set => m_Depth = value;
        }

        [Ignore]
        public TreeElement Parent
        {
            get => _parent;
            set => _parent = value;
        }

        [Ignore]
        public List<TreeElement> Children
        {
            get => _children;
            set => _children = value;
        }

        [Ignore] public bool HasChildren => Children != null && Children.Count > 0;

        [Ignore]
        public string TreeName
        {
            get => m_Name;
            set => m_Name = value;
        }

        [Ignore]
        public int TreeId
        {
            get => m_ID;
            set => m_ID = value;
        }

        public TreeElement()
        {
        }

        public TreeElement(string name, int depth, int id)
        {
            m_Name = name;
            m_ID = id;
            m_Depth = depth;
        }
    }
}