using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RefShortcuts.Editor
{
    public class RefShortcutEditorWindow : EditorWindow
    {
        private static EditorWindow _window;
        private DataContainer _dataContainer;
        private Object _newObject;
        private Vector2 _scrollPos = Vector2.zero;
        private int _currentTabIndex;
        private bool _settingsEnabled;
        private string _settingsNewTabName;
        private ReorderableList _tabItemsReorderableList;
        private ReorderableList _tabsReorderableList;
        private bool _disableListDraw;

        private List<ObjectContainer> CurrentDataList => _dataContainer.GetData(_currentTabIndex);

        [MenuItem("Window/Tools/RefShortcut", false, 1)]
        private static void ShowWindow()
        {
            _window = GetWindow(typeof(RefShortcutEditorWindow));
            _window.titleContent.text = "RefShortcuts";
        }

        private void Awake()
        {
            CacheShortcutData();
            RemoveEmptyElementsInData();
        }

        private void CacheShortcutData()
        {
            var configFilePath = $"{GetEditorScriptFilePath()}{StaticContent.SHORTCUT_DATA_FILE_NAME}";
            _dataContainer = (DataContainer) (AssetDatabase.LoadAssetAtPath(configFilePath, typeof(DataContainer)));

            if (_dataContainer == null)
            {
                AssetDatabase.CreateAsset(CreateInstance<DataContainer>(), configFilePath);
                AssetDatabase.SaveAssets();

                _dataContainer = (DataContainer) (AssetDatabase.LoadAssetAtPath(configFilePath, typeof(DataContainer)));
            }
        }

        private string GetEditorScriptFilePath()
        {
            var monoScript = MonoScript.FromScriptableObject(this);
            var scriptFilePath = AssetDatabase.GetAssetPath(monoScript);
            return scriptFilePath.Split(new[] {monoScript.name + ".cs"}, System.StringSplitOptions.None)[0];
        }

        private void RemoveEmptyElementsInData()
        {
            var list = CurrentDataList.Where(x => x.Object != null).Distinct().ToList();
            _dataContainer.SetDataList(list, _currentTabIndex);
            EditorUtility.SetDirty(_dataContainer);
        }

        private void OnGUI()
        {
            _disableListDraw = false;

            DrawHeader();

            if (!_settingsEnabled)
            {
                _scrollPos = DrawScroll(_scrollPos, position, () =>
                {
                    DrawTabs();

                    if (_tabItemsReorderableList == null)
                        IntTabItemsReorderList(CurrentDataList);

                    _tabItemsReorderableList?.DoLayoutList();
                });
            }
            else
            {
                _scrollPos = DrawScroll(_scrollPos, position, DrawSettings);
            }
        }

        private void DrawHeader()
        {
            var addObjFieldRect = new Rect(1f, 0f, (position.width - 2f), 22f);
            DrawBackgroundBox(addObjFieldRect, StaticContent.HEADER_COLOR);

            EditorGUILayout.BeginHorizontal(); //add object
            {
                var width = position.width;
                if (!_settingsEnabled)
                {
                    EditorGUIUtility.labelWidth = 115f;
                    _newObject = EditorGUILayout.ObjectField(StaticContent.DRAG_FIELD_LABEL,
                        _newObject,
                        typeof(Object),
                        true,
                        GUILayout.Width(width - 28));

                    if (CurrentDataList == null)
                    {
                        _currentTabIndex = 0;
                        ResetTabItemsList();
                    }

                    if (_newObject != null && !CurrentDataList.Exists(x => x.Object == _newObject))
                    {
                        var firstEmptyIndex = CurrentDataList.FindIndex(x => x.Object == null);
                        var newDataInfo = new ObjectContainer(_newObject);

                        if (firstEmptyIndex < 0)
                        {
                            CurrentDataList.Add(newDataInfo);
                        }
                        else
                        {
                            CurrentDataList[firstEmptyIndex] = newDataInfo;
                        }

                        EditorUtility.SetDirty(_dataContainer);
                    }

                    _newObject = null;

                    EditorGUILayout.BeginVertical(); // settings button
                    {
                        GUILayout.Space(3);
                        if (GUILayout.Button(StaticContent.SettingsIcon, new GUIStyle(), GUILayout.Height(StaticContent.SETTINGS_BUTTON_SIZE)))
                        {
                            _settingsEnabled = !_settingsEnabled;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(StaticContent.SETTINGS_TABS_LABEL, GUILayout.Width(width - 28));

                    EditorGUILayout.BeginVertical(); // close settings button
                    {
                        GUILayout.Space(3);
                        if (GUILayout.Button(StaticContent.CloseSettingsIcon, new GUIStyle(), GUILayout.Height(StaticContent.SETTINGS_BUTTON_SIZE)))
                        {
                            _settingsEnabled = !_settingsEnabled;
                        }
                    }
                }


                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            var tabs = _dataContainer.GetTabs();
            if (tabs.Length <= 1)
                return;

            var lastIndex = _currentTabIndex;

            GuiHorizontal(() =>
            {
                _currentTabIndex = GUILayout.Toolbar(_currentTabIndex, tabs);
            });

            if (lastIndex != _currentTabIndex)
            {
                IntTabItemsReorderList(CurrentDataList);
            }
        }

        private void DrawSettings()
        {
            GuiVertical(() =>
            {
                if (_tabsReorderableList == null)
                    IntSettingsTabsReorderList(_dataContainer.GetTabs());

                _tabsReorderableList?.DoLayoutList();
            });
        }

        private void IntTabItemsReorderList(IList list)
        {
            _tabItemsReorderableList = new ReorderableList(list, typeof(Object));
            _tabItemsReorderableList.drawElementCallback += DrawTabItemsReorderListElement;
            _tabItemsReorderableList.onReorderCallback += TabItemsReorderCallback;
            _tabItemsReorderableList.displayRemove = false;
            _tabItemsReorderableList.displayAdd = false;
            _tabItemsReorderableList.headerHeight = 0;
        }

        private void IntSettingsTabsReorderList(IList list)
        {
            _tabsReorderableList = new ReorderableList(list, typeof(string));
            _tabsReorderableList.drawElementCallback += DrawSettingsReorderListElement;
            _tabsReorderableList.onReorderCallback += SettingsTabsReorderCallback;
            _tabsReorderableList.onAddCallback += SettingsAddTabsCallback;
            _tabsReorderableList.displayRemove = false;
            _tabsReorderableList.headerHeight = 0;
        }

        private void SettingsAddTabsCallback(ReorderableList reorderableList)
        {
            const string tabName = StaticContent.NEW_TAB_NAME;
            var search = true;
            var index = 0;
            var newTabName = string.Empty;
            var tabs = _dataContainer.GetTabs();
            while (search)
            {
                newTabName = $"{tabName}{index++}";
                search = tabs.FirstOrDefault(x => x.Equals(newTabName)) != null;
            }

            _dataContainer.AddTab(newTabName);

            EditorUtility.SetDirty(_dataContainer);
            _tabsReorderableList = null;
        }

        private void SettingsTabsReorderCallback(ReorderableList reorderableList)
        {
            var array = reorderableList.list as string[];
            _dataContainer.ReorderTabs(array);

            EditorUtility.SetDirty(_dataContainer);

            _tabsReorderableList = null;
            _tabItemsReorderableList = null;
            _currentTabIndex = 0;
        }


        private void DrawSettingsReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (_disableListDraw)
                return;

            GuiHorizontal(() =>
            {
                var tab = _dataContainer.GetTabs()[index];
                var fieldRect = new Rect(rect.x, rect.y, position.width - StaticContent.FIELD_SIZE, EditorGUIUtility.singleLineHeight);
                var newTabName = EditorGUI.TextField(fieldRect, tab);

                if (!newTabName.Equals(tab, StringComparison.InvariantCulture))
                    _dataContainer.RenameTab(tab, newTabName);

                fieldRect.x = rect.x + position.width - StaticContent.REMOVE_BUTTON_OFFSET;
                fieldRect.width = StaticContent.REMOVE_BUTTON_SIZE;

                if (_dataContainer.Container.Count > 1)
                {
                    if (GUI.Button(fieldRect, StaticContent.CloseIcon))
                    {
                        _dataContainer.RemoveTab(tab);
                        _disableListDraw = true;
                        _tabsReorderableList = null;
                        _tabItemsReorderableList = null;

                        EditorUtility.SetDirty(_dataContainer);
                    }
                }
            });
        }

        private void TabItemsReorderCallback(ReorderableList reorderableList)
        {
            _dataContainer.SetDataList((List<ObjectContainer>) reorderableList.list, _currentTabIndex);
            EditorUtility.SetDirty(_dataContainer);
        }

        private void DrawTabItemsReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (_disableListDraw)
                return;

            GuiHorizontal(() =>
            {
                if(CurrentDataList.Count == 0)
                    return;
                
                var fieldRect = new Rect(rect.x, rect.y, position.width - StaticContent.FIELD_SIZE - StaticContent.REMOVE_BUTTON_SIZE, EditorGUIUtility.singleLineHeight);
                var curInfo = CurrentDataList.ElementAt(index);
                EditorGUI.ObjectField(fieldRect, curInfo.Object, typeof(Object), false);

                fieldRect.x = rect.x + position.width - StaticContent.REMOVE_BUTTON_OFFSET - StaticContent.REMOVE_BUTTON_SIZE;
                fieldRect.width = StaticContent.REMOVE_BUTTON_SIZE;
                if (GUI.Button(fieldRect, StaticContent.ShowIcon))
                {
                    var item = (ObjectContainer)_tabItemsReorderableList.list[index];
                    ObjectPropertyEditor.OpenInPropertyEditor(item.Object);
                }
                
                fieldRect.x = (rect.x + position.width) - StaticContent.REMOVE_BUTTON_OFFSET;
                fieldRect.width = StaticContent.REMOVE_BUTTON_SIZE;
                if (GUI.Button(fieldRect, StaticContent.RemoveIcon))
                {
                    RemoveTabItem(index);
                    _disableListDraw = true;
                }
            });
        }

        private void RemoveTabItem(int index)
        {
            CurrentDataList.RemoveAt(index);
            ResetTabItemsList();
            EditorUtility.SetDirty(_dataContainer);
        }

        private void ResetTabItemsList()
        {
            _tabItemsReorderableList = null;
            _disableListDraw = true;
        }

        private static void DrawBackgroundBox(Rect rect, Color color)
        {
            EditorGUI.HelpBox(rect, null, MessageType.None);
            EditorGUI.DrawRect(rect, color);
        }

        private static void GuiVertical(Action callback)
        {
            EditorGUILayout.BeginVertical();
            callback?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private static void GuiHorizontal(Action callback)
        {
            EditorGUILayout.BeginHorizontal();
            callback?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        private static Vector2 DrawScroll(Vector2 position, Rect windowRect, Action callback)
        {
            var resultPos = EditorGUILayout.BeginScrollView(position,
                GUILayout.Width(windowRect.width),
                GUILayout.Height(windowRect.height - 40f));
            {
                callback?.Invoke();
            }
            EditorGUILayout.EndScrollView();

            return resultPos;
        }
    }
    
    /*public static class OpenPropertiesEditorWindowDoubleClickListener
   {
      private static MethodInfo openPropertyEditorInfo;
      private static System.Type[] callTypes = new[] { typeof(Object), typeof(bool) };
      private static object[] callOpenBuffer = { null, true };
 
      /// <summary>
      /// Listens <see cref="OnOpenAssetAttribute"/> (order 100) for everything except folders.
      /// </summary>
      /// <param name="instanceID"><see cref="OnOpenAssetAttribute"/></param>
      /// <param name="line"><see cref="OnOpenAssetAttribute"/></param>
      /// <returns>True if opening the asset is handled</returns>
      [OnOpenAsset(100)]
      private static bool HandleOpenAsset(int instanceID, int line)
      {
         Object obj = EditorUtility.InstanceIDToObject(instanceID);
         if (obj == null)
         {
            return false;
         }
 
         if (IsFolder(obj))
         {
            return false;
         }
 
         return OpenInPropertyEditor(obj);
      }
 
      private static bool IsFolder(Object obj)
      {
         string assetPath = AssetDatabase.GetAssetPath(obj);
         return !string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath);
      }
 
      public static bool OpenInPropertyEditor(Object asset)
      {
         if (openPropertyEditorInfo == null)
         {
            System.Type propertyEditorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.PropertyEditor");
 
            // Get specific method, since there is an overload starting with Unity 2021.2
            openPropertyEditorInfo = propertyEditorType.GetMethod(
               "OpenPropertyEditor",
               BindingFlags.Static | BindingFlags.NonPublic,
               null,
               callTypes,
               null);
         }
 
 
         if (openPropertyEditorInfo != null)
         {
            callOpenBuffer[0] = asset;
            openPropertyEditorInfo.Invoke(null, callOpenBuffer);
            return true;
         }
 
         return false;
      }
   }*/
}