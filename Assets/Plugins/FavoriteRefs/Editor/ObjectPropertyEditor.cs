using System.Reflection;
using UnityEngine;

namespace FavoriteRefs.Editor
{
   public static class ObjectPropertyEditor
   {
      private static MethodInfo _openPropertyEditorInfo;
      private static readonly System.Type[] CallTypes = { typeof(Object), typeof(bool) };
      private static readonly object[] CallOpenBuffer = { null, true };

      public static bool OpenInPropertyEditor(Object asset)
      {
         if (_openPropertyEditorInfo == null)
         {
            var propertyEditorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PropertyEditor");

            // Get specific method, since there is an overload starting with Unity 2021.2
            _openPropertyEditorInfo = propertyEditorType.GetMethod(
               "OpenPropertyEditor",
               BindingFlags.Static | BindingFlags.NonPublic,
               null,
               CallTypes,
               null);
         }


         if (_openPropertyEditorInfo != null)
         {
            CallOpenBuffer[0] = asset;
            _openPropertyEditorInfo.Invoke(null, CallOpenBuffer);
            return true;
         }

         return false;
      }
   }
}