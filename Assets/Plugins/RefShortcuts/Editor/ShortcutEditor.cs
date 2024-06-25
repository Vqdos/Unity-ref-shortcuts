using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RefShortcuts.Editor
{
    public class ShortcutEditor : EditorWindow
    {
        private static EditorWindow _window = null;

        private ShortcutData _shortcutData;
        private Object _newObject;
        private Vector2 _scrollPos = Vector2.zero;
        private int _currentTabIndex;
        private bool _settingsEnabled;
        private string _settingsNewTabName;
        private ReorderableList _tabItemsReorderableList;
        private ReorderableList _tabsReorderableList;
        private bool _disableListDraw;

        private List<ShortcutDataContainer> CurrentDataList => _shortcutData.GetData(_currentTabIndex);

        [MenuItem("Window/Tools/RefShortcut", false, 1)]
        private static void ShowWindow()
        {
            _window = GetWindow(typeof(ShortcutEditor));
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
            _shortcutData = (ShortcutData)(AssetDatabase.LoadAssetAtPath(configFilePath, typeof(ShortcutData)));

            if (_shortcutData == null)
            {
                AssetDatabase.CreateAsset(CreateInstance<ShortcutData>(), configFilePath);
                AssetDatabase.SaveAssets();

                _shortcutData = (ShortcutData)(AssetDatabase.LoadAssetAtPath(configFilePath, typeof(ShortcutData)));
            }
        }

        private string GetEditorScriptFilePath()
        {
            var monoScript = MonoScript.FromScriptableObject(this);
            var scriptFilePath = AssetDatabase.GetAssetPath(monoScript);
            return scriptFilePath.Split(new[] { monoScript.name + ".cs" }, System.StringSplitOptions.None)[0];
        }

        private void RemoveEmptyElementsInData()
        {
            var list = CurrentDataList.Where(x => x.Object != null).Distinct().ToList();
            _shortcutData.SetDataList(list, _currentTabIndex);
            EditorUtility.SetDirty(_shortcutData);
        }

        private void OnGUI()
        {
            _disableListDraw = false;

            DrawHeader();

            if (!_settingsEnabled)
            {
                DrawScrollContent(() =>
                {
                    DrawTabs();
                    
                    if (_tabItemsReorderableList == null)
                        IntTabItemsReorderableList(CurrentDataList);
                    
                    _tabItemsReorderableList?.DoLayoutList();
                });
            }
            else
            {
                DrawSettings();
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
                    _newObject = EditorGUILayout.ObjectField(StaticContent.DRAG_FIELD_LABEL, _newObject, typeof(Object), true, GUILayout.Width(width - 28));

                    if (_newObject != null && !CurrentDataList.Exists(x => x.Object == _newObject))
                    {
                        var firstEmptyIndex = CurrentDataList.FindIndex(x => x.Object == null);
                        var newDataInfo = new ShortcutDataContainer(_newObject);

                        if (firstEmptyIndex < 0)
                        {
                            CurrentDataList.Add(newDataInfo);
                        }
                        else
                        {
                            CurrentDataList[firstEmptyIndex] = newDataInfo;
                        }

                        EditorUtility.SetDirty(_shortcutData);
                    }
                    
                    _newObject = null;
       
                    EditorGUILayout.BeginVertical(); // settings button
                    {
                        GUILayout.Space(3);
                        if (GUILayout.Button(StaticContent.SettingsIcon, new GUIStyle(), GUILayout.Height(20)))
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
                        if (GUILayout.Button(StaticContent.CloseSettingsIcon, new GUIStyle(), GUILayout.Height(20)))
                        {
                            _settingsEnabled = !_settingsEnabled;
                        }
                    }
                }

                
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScrollContent(Action callback)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height - 40f));
            {
               callback?.Invoke();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawTabs()
        {
            var tabs = _shortcutData.GetTabs();
            if (tabs.Length <= 1)
                return;

            var lastIndex = _currentTabIndex;

            GuiHorizontal(() =>
            {
                _currentTabIndex = GUILayout.Toolbar(_currentTabIndex, tabs);
            });

            if (lastIndex != _currentTabIndex)
            {
                IntTabItemsReorderableList(CurrentDataList);
            }
        }

        private void DrawSettings()
        {
            GuiVertical(() =>
            {
                if (_tabsReorderableList == null)
                    IntSettingsTabsReorderableList(_shortcutData.GetTabs());

                _tabsReorderableList?.DoLayoutList();
            });
        }

        private void IntTabItemsReorderableList(IList list)
        {
            _tabItemsReorderableList = new ReorderableList(list, typeof(Object));
            _tabItemsReorderableList.drawElementCallback += DrawTabItemsReorderListElement;
            _tabItemsReorderableList.onReorderCallback += TabItemsReorderCallback;
            _tabItemsReorderableList.displayRemove = false;
            _tabItemsReorderableList.displayAdd = false;
            _tabItemsReorderableList.headerHeight = 0; 
        }

        private void IntSettingsTabsReorderableList(IList list)
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
            var tabs = _shortcutData.GetTabs();
            while (search)
            {
                newTabName = $"{tabName}{index++}";
                search = tabs.FirstOrDefault(x => x.Equals(newTabName)) != null;
            }
            
            _shortcutData.AddTab(newTabName);
            
            EditorUtility.SetDirty(_shortcutData);
            _tabsReorderableList = null;
        }

        private void SettingsTabsReorderCallback(ReorderableList reorderableList)
        {
            var array = reorderableList.list as string[];
            _shortcutData.ReorderTabs(array);
            
            EditorUtility.SetDirty(_shortcutData);
            
            _tabsReorderableList = null;
            _tabItemsReorderableList = null;
            _currentTabIndex = 0;
        }

        
        private void DrawSettingsReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(_disableListDraw)
                return;

            GuiHorizontal(() =>
            {
                var tab = _shortcutData.GetTabs()[index];
                var fieldRect = new Rect(rect.x, rect.y, position.width - 50, EditorGUIUtility.singleLineHeight);
                var newTabName = EditorGUI.TextField(fieldRect, tab);

                if (!newTabName.Equals(tab, StringComparison.InvariantCulture))
                    _shortcutData.RenameTab(tab, newTabName);

                fieldRect.x = rect.x + position.width - 45;
                fieldRect.width = 20;
                if (GUI.Button(fieldRect, StaticContent.CloseIcon))
                {
                    _shortcutData.RemoveTab(tab);
                    _disableListDraw = true;
                }
            });
        }

        private void TabItemsReorderCallback(ReorderableList reorderableList)
        {
            _shortcutData.SetDataList((List<ShortcutDataContainer>)reorderableList.list, _currentTabIndex);
            EditorUtility.SetDirty(_shortcutData);
        }

        private void DrawTabItemsReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(_disableListDraw)
                return;

            GuiHorizontal(() =>
            {
                var fieldRect = new Rect(rect.x, rect.y, position.width - 50, EditorGUIUtility.singleLineHeight);
                var curInfo = CurrentDataList.ElementAt(index);
                EditorGUI.ObjectField(fieldRect, curInfo.Object, typeof(Object),false);
                
                fieldRect.x = rect.x + position.width - 45;
                fieldRect.width = 20;
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

            _tabItemsReorderableList = null;
            EditorUtility.SetDirty(_shortcutData);
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
    }
}