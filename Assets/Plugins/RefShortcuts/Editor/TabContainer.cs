using System;
using System.Collections.Generic;
using UnityEngine;

namespace RefShortcuts.Editor
{
    [Serializable]
    public class TabContainer
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public List<ObjectContainer> DataList { get; private set; } = new List<ObjectContainer>();

        public TabContainer(string name)
        {
            Name = name;
        }

        public void SetDataList(List<ObjectContainer> list)
        {
            DataList = list;
        }

        public void SetName(string name)
        {
            Name = name;
        }
    }
}