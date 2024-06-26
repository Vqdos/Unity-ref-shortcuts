using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RefShortcuts.Editor
{
    [Serializable]
    public class ObjectContainer
    {
        [field: SerializeField] public Object @Object { get; private set; }

        public ObjectContainer(Object @object)
        {
            Debug.Assert(@object != null, "Object can't be null.");
            Object = @object;
        }

        public void SetObject(Object @object)
        {
            Object = @object;
        }
    }
}