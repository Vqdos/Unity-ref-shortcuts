using System;
using System.Collections.Generic;
using UnityEngine;

namespace RefShortcuts.Editor
{
    [Serializable]
    public class TabContainer
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public List<ShortcutDataContainer> DataList { get; private set; } = new List<ShortcutDataContainer>();

        public TabContainer(string name)
        {
            Name = name;
        }
        public void SetDataList(List<ShortcutDataContainer> list)
        {
            DataList = list;
        }
        public void SetName(string name)
        {
            Name = name;
        }
    }
}