using System.Collections.Generic;
using System.Linq;
using Plugins.RefShortcuts.Scripts;
using UnityEditor;
using UnityEngine;

namespace Plugins.RefShortcuts.Editor
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
        
        private List<ShortcutDataContainer> CurrentDataList => _shortcutData.GetData(_currentTabIndex);
        private int _currentTabIndex = 0;
        private bool _settingsEnabled;
        private string _settingsNewTabNAme;

        [MenuItem("Window/Tools/RefShortcut", false, 1)]
        private static void ShowWindow()
        {
            _window = GetWindow(typeof(ShortcutEditor));
            _window.titleContent.text = "RefShortcuts";
        }

        private void OnEnable()
        {
            CacheShortcutData();
            RemoveEmptyElementsInData();
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
            List<ShortcutDataContainer> dataInfoList = CurrentDataList;
            var tabs = _shortcutData.GetTabs();

            #region Add Shortcut

            var addObjFieldRect = new Rect(1f, 0f, (position.width - 2f), 22f);
            DrawBackgroundBox(addObjFieldRect, _addFieldColor);

            EditorGUILayout.BeginHorizontal();
            {
                var width = position.width;
                EditorGUIUtility.labelWidth = 115f;
                _newObject = EditorGUILayout.ObjectField("Drag anything here:", _newObject, typeof(Object), true, GUILayout.Width(width - 28));

                if (_newObject != null && !dataInfoList.Exists(x => x.Object == _newObject))
                {
                    var firstEmptyIndex = dataInfoList.FindIndex(x => x.Object == null);
                    var newDataInfo = new ShortcutDataContainer(_newObject);

                    if (firstEmptyIndex < 0)
                    {
                        dataInfoList.Add(newDataInfo);
                    }
                    else
                    {
                        dataInfoList[firstEmptyIndex] = newDataInfo;
                    }

                    EditorUtility.SetDirty(_shortcutData);
                }

                _newObject = null;
       
                EditorGUILayout.BeginVertical();
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

            var listFieldRect = new Rect(1f, 21f, (position.width - 2f), position.height);
            DrawBackgroundBox(listFieldRect, _listFieldColor);
            
            if (!_settingsEnabled)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width),
                    GUILayout.Height(position.height - 40f));
                {
                    if (tabs.Length > 1)
                    {
                        EditorGUILayout.BeginHorizontal();
                        _currentTabIndex = GUILayout.Toolbar(_currentTabIndex, tabs);
                        EditorGUILayout.EndHorizontal();
                    }

                    var deleteIdx = -1;

                    for (var i = 0; i < dataInfoList.Count; ++i)
                    {
                        var curInfo = dataInfoList[i];

                        if (curInfo.Object == null)
                        {
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        {

                            EditorGUI.BeginChangeCheck();
                            {
                                EditorGUILayout.LabelField("", GUILayout.Width(5f));

                                var obj = EditorGUILayout.ObjectField(curInfo.Object, typeof(Object), true);
                                curInfo.SetObject(obj);
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorUtility.SetDirty(_shortcutData);
                            }
                            else if (GUILayout.Button("X", GUILayout.Width(20f)))
                            {
                                deleteIdx = i;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (deleteIdx >= 0)
                    {
                        dataInfoList.RemoveAt(deleteIdx);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                DrawSettings(tabs);
            }

            #endregion Show list
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
                                break;
                        }
                    
                        if(i != 0)
                        {
                            if (GUILayout.Button("X", GUILayout.Width(20f)))
                            {
                                Debug.LogError("Remove");
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
                    _settingsNewTabNAme = GUILayout.TextField(_settingsNewTabNAme);
            
                    if (GUILayout.Button("+", GUILayout.Width(20f)))
                    {
                        Debug.LogError("Add");
                        _shortcutData.AddTab(_settingsNewTabNAme);
                    }   
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawBackgroundBox(Rect rect, Color color)
        {
            EditorGUI.HelpBox(rect, null, MessageType.None);
            EditorGUI.DrawRect(rect, color);
        }
    }
}