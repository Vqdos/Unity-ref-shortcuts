using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RefShortcuts.Editor
{
    public class DataContainer : ScriptableObject
    {
        [field: SerializeField] public List<TabContainer> Container { get; private set; } = new List<TabContainer>
        {
            new TabContainer("Temp")
        };

        public List<ObjectContainer> GetData(int index)
        {
            return index >= Container.Count ? null : Container[index].DataList;
        }

        public void SetDataList(List<ObjectContainer> list, int index)
        {
            Container[index].SetDataList(list);
        }

        public string[] GetTabs()
        {
            return Container.Select(tab => tab.Name).ToArray();
        }

        public bool AddTab(string name)
        {
            if (Container.FirstOrDefault(x => x.Name == name) != null)
                return false;
            
            Container.Add(new TabContainer(name));
            return true;
        }

        public bool RenameTab(string from, string to)
        {
            var item = Container.FirstOrDefault(x => x.Name.Equals(from));

            if (item == null)
                return false;
            
            item.SetName(to);

            return true;
        }

        public void RemoveTab(string name)
        {
            Container.RemoveAll(x => x.Name == name);
        }

        public void ReorderTabs(IEnumerable<string> array)
        {
            var source = new List<TabContainer>(Container);
            Container = new List<TabContainer>();
            
            foreach (var tab in array)
            {
                Container.Add(source.First(x=>x.Name.Equals(tab)));
            }
        }
    }
}