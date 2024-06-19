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
        private const string SHORTCUT_DATA_FILE_NAME = "RefShortcutData.asset";

        private static EditorWindow _window = null;
        private readonly Color _addFieldColor = new Color(0f, 0.25f, 0f, 0.2f);
        private readonly Color _listFieldColor = new Color(0f, 0f, 0f, 0.2f);

        private ShortcutData _shortcutData;
        private Object _newObject;
        private Vector2 _scrollPos = Vector2.zero;
        private int _currentTabIndex;
        private bool _settingsEnabled;
        private string _settingsNewTabName;
        private ReorderableList _reorderableList;
        
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
            IntReorderableList(CurrentDataList);
        }

        private void CacheShortcutData()
        {
            var configFilePath = $"{GetEditorScriptFilePath()}{SHORTCUT_DATA_FILE_NAME}";
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
            _deleteItemIndex = -1;
            var dataList = CurrentDataList;
            var tabs = _shortcutData.GetTabs();

            #region Add Shortcut

            var addObjFieldRect = new Rect(1f, 0f, (position.width - 2f), 22f);
            DrawBackgroundBox(addObjFieldRect, _addFieldColor);

            EditorGUILayout.BeginHorizontal(); //add object
            {
                var width = position.width;
                EditorGUIUtility.labelWidth = 115f;
                _newObject = EditorGUILayout.ObjectField("Drag anything here:", _newObject, typeof(Object), true, GUILayout.Width(width - 28));

                if (_newObject != null && !dataList.Exists(x => x.Object == _newObject))
                {
                    var firstEmptyIndex = dataList.FindIndex(x => x.Object == null);
                    var newDataInfo = new ShortcutDataContainer(_newObject);

                    if (firstEmptyIndex < 0)
                    {
                        dataList.Add(newDataInfo);
                    }
                    else
                    {
                        dataList[firstEmptyIndex] = newDataInfo;
                    }

                    EditorUtility.SetDirty(_shortcutData);
                }

                _newObject = null;
       
                EditorGUILayout.BeginVertical(); // settings button
                {
                    GUILayout.Space(3);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), new GUIStyle(), GUILayout.Height(20)))
                    {
                        _settingsEnabled = !_settingsEnabled;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            #endregion Add Shortcut

            #region Show list

            /*var listFieldRect = new Rect(1f, 21f, (position.width - 2f), position.height);
            DrawBackgroundBox(listFieldRect, _listFieldColor);*/
            
            if (!_settingsEnabled)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height - 40f));
                {
                    DrawTabs(tabs);
                    _reorderableList?.DoLayoutList();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                DrawSettings(tabs);
            }

            #endregion Show list
        }

        private void DrawTabs(string[] tabs)
        {
            if (tabs.Length <= 1)
                return;

            var lastIndex = _currentTabIndex;

            EditorGUILayout.BeginHorizontal();
            _currentTabIndex = GUILayout.Toolbar(_currentTabIndex, tabs);
            EditorGUILayout.EndHorizontal();

            if (lastIndex != _currentTabIndex)
            {
                IntReorderableList(CurrentDataList);
            }
        }
        
        private void DrawSettings(IReadOnlyList<string> tabs)
        {
            EditorGUILayout.BeginVertical();
            {
                for (var i = 0; i < tabs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        var tabName= tabs[i];
                        var newName = GUILayout.TextField(tabName);

                        if (tabName != newName)
                        {
                            if (!_shortcutData.RenameTab(tabName, newName))
                            {
                                Debug.LogError($"{nameof(ShortcutData)}: tab \"{tabName}\" dont found");
                                break;
                            }
                        }
                    
                        if(i != 0)
                        {
                            if (GUILayout.Button("X", GUILayout.Width(20f)))
                            {
                                if (_currentTabIndex == i)
                                    _currentTabIndex = 0;
                        
                                _shortcutData.RemoveTab(tabName);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                GUILayout.Space(10);
                GUILayout.Label("Add new tab:", GUILayout.Width(90f));

                EditorGUILayout.BeginHorizontal();
                {
                    _settingsNewTabName = GUILayout.TextField(_settingsNewTabName);
            
                    if (GUILayout.Button("+", GUILayout.Width(20f)))
                    {
                        if(!_shortcutData.AddTab(_settingsNewTabName))
                            Debug.LogError($"{nameof(ShortcutData)}: tab \"{_settingsNewTabName}\" already exist");
                    }   
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void IntReorderableList(IList list)
        {
            _reorderableList = new ReorderableList(list, typeof(Object));
            _reorderableList.drawElementCallback += DrawReorderListElement;
            _reorderableList.onReorderCallback += ReorderCallback;
            _reorderableList.displayRemove = false;
            _reorderableList.displayAdd = false;
            _reorderableList.headerHeight = 0;
        }

        private void ReorderCallback(ReorderableList reorderableList)
        {
            _shortcutData.SetDataList((List<ShortcutDataContainer>)reorderableList.list, _currentTabIndex);
            EditorUtility.SetDirty(_shortcutData);
        }

        private int _deleteItemIndex = -1;
        private void DrawReorderListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(_deleteItemIndex >= 0)
                return;
            
            EditorGUILayout.BeginHorizontal();
            {
                var objRect = new Rect(rect.x, rect.y, position.width - 50, EditorGUIUtility.singleLineHeight);
                var curInfo = CurrentDataList.ElementAt(index);
                EditorGUI.ObjectField(objRect, curInfo.Object, typeof(Object),false);
                
                objRect.x = rect.x + position.width - 45;
                objRect.width = 20;
                if (GUI.Button(objRect, EditorGUIUtility.IconContent("d_Toolbar Minus")))
                {
                    _deleteItemIndex = index;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (_deleteItemIndex >= 0)
            {
                RemoveItem(_deleteItemIndex);
            }
        }

        private void RemoveItem(int index)
        {
            CurrentDataList.RemoveAt(index);
            IntReorderableList(CurrentDataList);
            EditorUtility.SetDirty(_shortcutData);
        }

        private static void DrawBackgroundBox(Rect rect, Color color)
        {
            EditorGUI.HelpBox(rect, null, MessageType.None);
            EditorGUI.DrawRect(rect, color);
        }
    }
}