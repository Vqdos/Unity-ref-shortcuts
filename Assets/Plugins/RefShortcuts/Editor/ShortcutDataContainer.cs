using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RefShortcuts.Editor
{
    [Serializable]
    public class ShortcutDataContainer
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Object @Object { get; private set; }

        public ShortcutDataContainer(Object @object)
        {
            Debug.Assert(@object != null, "Object can't be null.");

            Name = @object.name;
            Object = @object;
        }

        public void SetObject(Object @object)
        {
            Name = @object.name;
            Object = @object;
        }
    }
}