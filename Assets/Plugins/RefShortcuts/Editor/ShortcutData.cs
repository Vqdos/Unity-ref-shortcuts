using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RefShortcuts.Editor
{
    public class ShortcutData : ScriptableObject
    {
        public List<ShortcutDataContainer> GetData(int index)
        {
            return index >= Container.Count ? null : Container[index].DataList;
        }

        [field: SerializeField] public List<TabContainer> Container { get; private set; } = new List<TabContainer>
        {
            new TabContainer("Temp")
        };

        public void SetDataList(List<ShortcutDataContainer> list, int index)
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

        public bool RenameTab(string fromName, string toName)
        {
            var item = Container.FirstOrDefault(x => x.Name.Equals(fromName));

            if (item == null)
                return false;
            
            item.SetName(toName);

            return true;
        }

        public void RemoveTab(string name)
        {
            Container.RemoveAll(x => x.Name == name);
        }
    }
}