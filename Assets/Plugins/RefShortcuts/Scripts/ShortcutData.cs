using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plugins.RefShortcuts.Scripts
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

        public void AddTab(string key)
        {
            Container.Add(new TabContainer(key));
        }

        public bool RenameTab(string fromKey, string toKey)
        {
            var item = Container.FirstOrDefault(x => x.Name.Equals(fromKey));

            if (item == null)
            {
                Debug.LogError($"{nameof(ShortcutData)}: tab \"{fromKey}\" dont found");
                return false;
            }

            item.SetName(toKey);

            return true;
        }

        public void RemoveTab(string key)
        {
            Container.RemoveAll(x => x.Name == key);
        }
    }
}