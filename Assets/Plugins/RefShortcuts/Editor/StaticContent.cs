using UnityEditor;
using UnityEngine;

namespace RefShortcuts.Editor
{
    public static class StaticContent
    {
        public static readonly GUIContent CloseSettingsIcon = EditorGUIUtility.IconContent("d_winbtn_win_close");
        public static readonly GUIContent CloseIcon = EditorGUIUtility.IconContent("d_Toolbar Minus");
        public static readonly GUIContent SettingsIcon = EditorGUIUtility.IconContent("_Popup");
        public static GUIContent RemoveIcon => CloseIcon;

        public const string DRAG_FIELD_LABEL = "Drag anything here:";
        public const string NEW_TAB_NAME = "Tab";
        public const string SHORTCUT_DATA_FILE_NAME = "RefShortcutData.asset";
        public const string SETTINGS_TABS_LABEL = "Tabs:";
        
        public static readonly Color HEADER_COLOR = new Color(0f, 0.25f, 0f, 0.2f);

        public const int REMOVE_BUTTON_SIZE = 20;
        public const int SETTINGS_BUTTON_SIZE = 20;
        public const int FIELD_SIZE = 60;
        public const int REMOVE_BUTTON_OFFSET = 58;
    }
}