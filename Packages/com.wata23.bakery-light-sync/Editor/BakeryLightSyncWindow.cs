
//License: PolyForm Noncommercial 1.0.0

//Required Notice: Copyright(c) 2026 Wata23

#if UNITY_EDITOR

namespace Wata23.BakeryLightSync.Editor
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    // Main window, UI, scanning, and list rendering.
    public partial class BakeryLightSyncWindow : EditorWindow
    {
        private const string WindowTitle = "Bakery Light Sync";
        private const string MenuPath = "Tools/Bakery Light Sync";
        private const string LanguagePrefKey = "BakeryLightSyncWindow.Language";
        private const string RealtimeAddSettingPrefKey = "BakeryLightSyncWindow.RealtimeAddSetting";

        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private enum UiLanguage
        {
            English = 0,
            Japanese = 1
        }

        private enum RealtimeAddSetting
        {
            KeepRealtime = 0,
            AutoMixed = 1,
            AutoBaked = 2,
            AskOnAddAll = 3
        }

        private enum RealtimeResolvedAction
        {
            KeepRealtime = 0,
            ChangeToMixed = 1,
            ChangeToBaked = 2,
            Cancel = 3
        }

        // One row group for one Unity Light.
        private sealed class LightEntry
        {
            public Light UnityLight;
            public GameObject GameObject;
            public LightType UnityLightType;
            public Type BakeryType;
            public Component BakeryComponent;
            public bool IsSupported;
            public string Error;
        }

        // Sort rows close to the Hierarchy order.
        private sealed class HierarchyOrderKeyComparer : IComparer<List<int>>
        {
            public static readonly HierarchyOrderKeyComparer Instance = new HierarchyOrderKeyComparer();

            public int Compare(List<int> a, List<int> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return 0;
                }

                if (a == null)
                {
                    return -1;
                }

                if (b == null)
                {
                    return 1;
                }

                int count = Mathf.Min(a.Count, b.Count);

                for (int i = 0; i < count; i++)
                {
                    int cmp = a[i].CompareTo(b[i]);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                }

                return a.Count.CompareTo(b.Count);
            }
        }

        private static readonly LightmapBakeType[] LightModeValues =
        {
        LightmapBakeType.Realtime,
        LightmapBakeType.Mixed,
        LightmapBakeType.Baked
    };

        private UiLanguage language;
        private RealtimeAddSetting realtimeAddSetting;
        private string message;
        private GUIStyle messageBoxStyle;
        private Vector2 scroll;
        private readonly List<LightEntry> entries = new List<LightEntry>();
        private bool isBakeryAvailable = true;

        private Type bakeryPointLightType;
        private Type bakeryDirectLightType;
        private Type bakeryLightMeshType;
        private Type ftLightmapsType;

        private Texture2D defaultSpotCookie;
        private Material defaultAreaMaterial;

        private const float RowHeight = 22f;
        private const float CheckWidth = 28f;
        private const float ObjectWidth = 190f;
        private const float ComponentWidth = 170f;
        private const float TypeWidth = 95f;
        private const float LightModeWidth = 110f;
        private const float EnabledWidth = 70f;
        private const float ColorWidth = 105f;
        private const float IntensityWidth = 80f;
        private const float RangeWidth = 90f;
        private const float SpotAngleWidth = 90f;
        private const float AreaSizeWidth = 105f;
        private const float CookieWidth = 150f;

        private static readonly Color HeaderColor = new Color(0.18f, 0.18f, 0.18f, 0.22f);
        private static readonly Color RowSeparatorColor = new Color(0f, 0f, 0f, 0.16f);

        private float TotalTableWidth =>
            CheckWidth + ObjectWidth + ComponentWidth + TypeWidth + LightModeWidth + EnabledWidth +
            ColorWidth + IntensityWidth + RangeWidth + SpotAngleWidth + AreaSizeWidth + CookieWidth;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<BakeryLightSyncWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(1100, 360);
            window.Show();
        }

        private void OnEnable()
        {
            language = (UiLanguage)EditorPrefs.GetInt(LanguagePrefKey, (int)UiLanguage.English);
            realtimeAddSetting = (RealtimeAddSetting)EditorPrefs.GetInt(RealtimeAddSettingPrefKey, (int)RealtimeAddSetting.AskOnAddAll);
            SetMessage("Ready");
            RefreshLightList(false);
        }

        private void OnFocus()
        {
            RefreshLightList(false);
        }

        private void OnGUI()
        {
            DrawTopArea();
            DrawListArea();
        }

        // Draw the fixed controls at the top.
        private void DrawTopArea()
        {
            EditorGUILayout.Space(4);

            GUIStyle style = GetMessageBoxStyle();
            float viewWidth = position.width - 24f;
            float messageHeight = style.CalcHeight(new GUIContent(message ?? string.Empty), viewWidth);
            float minHeight = 44f;
            float finalHeight = Mathf.Max(minHeight, messageHeight + 8f);

            Rect messageRect = EditorGUILayout.GetControlRect(false, finalHeight);
            GUI.Label(messageRect, message ?? string.Empty, style);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(T("Language"), GUILayout.Width(75));

                var newLanguage = (UiLanguage)EditorGUILayout.EnumPopup(language, GUILayout.Width(120));
                if (newLanguage != language)
                {
                    language = newLanguage;
                    EditorPrefs.SetInt(LanguagePrefKey, (int)language);
                    SetMessage("LanguageChanged");
                }

                GUILayout.Space(10);

                if (GUILayout.Button(T("Refresh"), GUILayout.Width(110)))
                {
                    RefreshLightList(true);
                }

                EditorGUI.BeginDisabledGroup(!isBakeryAvailable);

                if (GUILayout.Button(T("AddAll"), GUILayout.Width(230)))
                {
                    AddBakeryComponentsToAllWithDialog();
                }

                if (GUILayout.Button(T("RemoveAll"), GUILayout.Width(250)))
                {
                    RemoveBakeryComponentsFromAllWithDialog();
                }

                Rect settingsRect = GUILayoutUtility.GetRect(new GUIContent(T("Settings")), EditorStyles.miniButton, GUILayout.Width(120));
                if (GUI.Button(settingsRect, T("Settings")))
                {
                    ShowSettingsMenu(settingsRect);
                }

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space(4);
        }

        // Show the settings popup menu.
        private void ShowSettingsMenu(Rect buttonRect)
        {
            GenericMenu menu = new GenericMenu();
            string root = T("RealtimeHandlingMenuRoot");

            AddRealtimeHandlingMenuItem(menu, root, RealtimeAddSetting.KeepRealtime);
            AddRealtimeHandlingMenuItem(menu, root, RealtimeAddSetting.AutoMixed);
            AddRealtimeHandlingMenuItem(menu, root, RealtimeAddSetting.AutoBaked);
            AddRealtimeHandlingMenuItem(menu, root, RealtimeAddSetting.AskOnAddAll);

            menu.DropDown(buttonRect);
        }

        // Add one menu item for the current handling mode.
        private void AddRealtimeHandlingMenuItem(GenericMenu menu, string root, RealtimeAddSetting setting)
        {
            GUIContent content = new GUIContent(root + "/" + GetRealtimeAddSettingLabel(setting));
            bool isOn = realtimeAddSetting == setting;

            menu.AddItem(content, isOn, () =>
            {
                realtimeAddSetting = setting;
                EditorPrefs.SetInt(RealtimeAddSettingPrefKey, (int)realtimeAddSetting);
                SetMessage("RealtimeHandlingChanged", GetRealtimeAddSettingLabel(setting));
            });
        }

        // Get the localized label for the handling mode.
        private string GetRealtimeAddSettingLabel(RealtimeAddSetting setting)
        {
            switch (setting)
            {
                case RealtimeAddSetting.KeepRealtime:
                    return T("RealtimeSettingKeep");
                case RealtimeAddSetting.AutoMixed:
                    return T("RealtimeSettingAutoMixed");
                case RealtimeAddSetting.AutoBaked:
                    return T("RealtimeSettingAutoBaked");
                case RealtimeAddSetting.AskOnAddAll:
                    return T("RealtimeSettingAskOnAddAll");
                default:
                    return setting.ToString();
            }
        }

        // Draw the scrollable list area.
        private void DrawListArea()
        {
            if (!isBakeryAvailable)
            {
                EditorGUILayout.HelpBox(T("BakeryNotInstalled"), MessageType.Warning);
                return;
            }

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox(T("NoLights"), MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawHeaderRow();

            foreach (var entry in entries)
            {
                DrawUnityLightRow(entry);

                if (entry.BakeryComponent != null)
                {
                    DrawBakeryRow(entry);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        // Draw the header row.
        private void DrawHeaderRow()
        {
            Rect rowRect = GUILayoutUtility.GetRect(TotalTableWidth, RowHeight, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rowRect, HeaderColor);

            float x = rowRect.x;

            DrawHeaderCell(ref x, rowRect.y, CheckWidth, string.Empty);
            DrawHeaderCell(ref x, rowRect.y, ObjectWidth, T("Object"));
            DrawHeaderCell(ref x, rowRect.y, ComponentWidth, T("Component"));
            DrawHeaderCell(ref x, rowRect.y, TypeWidth, T("LightType"));
            DrawHeaderCell(ref x, rowRect.y, LightModeWidth, T("LightMode"));
            DrawHeaderCell(ref x, rowRect.y, EnabledWidth, T("Enabled"));
            DrawHeaderCell(ref x, rowRect.y, ColorWidth, T("Color"));
            DrawHeaderCell(ref x, rowRect.y, IntensityWidth, T("Intensity"));
            DrawHeaderCell(ref x, rowRect.y, RangeWidth, T("Range"));
            DrawHeaderCell(ref x, rowRect.y, SpotAngleWidth, T("SpotAngle"));
            DrawHeaderCell(ref x, rowRect.y, AreaSizeWidth, T("AreaSize"));
            DrawHeaderCell(ref x, rowRect.y, CookieWidth, T("Cookie"));

            DrawRowSeparator(rowRect);
        }

        private void DrawHeaderCell(ref float x, float y, float width, string text)
        {
            Rect rect = new Rect(x + 2, y + 2, width - 4, RowHeight - 4);
            EditorGUI.LabelField(rect, text, EditorStyles.boldLabel);
            x += width;
        }

        // Draw one Unity Light row.
        private void DrawUnityLightRow(LightEntry entry)
        {
            Rect rowRect = GUILayoutUtility.GetRect(TotalTableWidth, RowHeight, GUILayout.ExpandWidth(false));
            float x = rowRect.x;

            Rect checkRect = NextRect(ref x, rowRect.y, CheckWidth);
            bool hasBakery = entry.BakeryComponent != null;

            EditorGUI.BeginDisabledGroup(!entry.IsSupported);
            bool newHasBakery = EditorGUI.Toggle(checkRect, hasBakery);
            EditorGUI.EndDisabledGroup();

            if (entry.IsSupported && newHasBakery != hasBakery)
            {
                if (newHasBakery)
                {
                    AddOrUpdateBakeryForEntry(entry, true, GetRealtimeResolvedActionForSingleAdd());
                }
                else
                {
                    RemoveBakeryForEntry(entry, true);
                }

                RefreshLightList(false);
                GUIUtility.ExitGUI();
                return;
            }

            Rect objectRect = NextRect(ref x, rowRect.y, ObjectWidth);
            EditorGUI.ObjectField(objectRect, entry.GameObject, typeof(GameObject), true);

            DrawLabelCell(ref x, rowRect.y, ComponentWidth, "Unity Light");
            DrawLabelCell(ref x, rowRect.y, TypeWidth, entry.UnityLightType.ToString());

            Rect lightModeRect = NextRect(ref x, rowRect.y, LightModeWidth);
            DrawLightModePopup(entry, lightModeRect);

            DrawLabelCell(ref x, rowRect.y, EnabledWidth, entry.UnityLight != null ? entry.UnityLight.enabled.ToString() : string.Empty);
            DrawLabelCell(ref x, rowRect.y, ColorWidth, ColorToString(entry.UnityLight != null ? entry.UnityLight.color : default));
            DrawLabelCell(ref x, rowRect.y, IntensityWidth, FloatToString(entry.UnityLight != null ? entry.UnityLight.intensity : 0f));
            DrawLabelCell(ref x, rowRect.y, RangeWidth, GetUnityRangeText(entry.UnityLight));
            DrawLabelCell(ref x, rowRect.y, SpotAngleWidth, GetUnitySpotAngleText(entry.UnityLight));
            DrawLabelCell(ref x, rowRect.y, AreaSizeWidth, GetUnityAreaSizeText(entry.UnityLight));
            DrawLabelCell(ref x, rowRect.y, CookieWidth, GetObjectName(entry.UnityLight != null ? entry.UnityLight.cookie : null));

            DrawRowSeparator(rowRect);
            HandleRowSelection(rowRect, entry.GameObject, excludeCheckBox: true);
        }

        // Draw one Bakery component row.
        private void DrawBakeryRow(LightEntry entry)
        {
            Rect rowRect = GUILayoutUtility.GetRect(TotalTableWidth, RowHeight, GUILayout.ExpandWidth(false));
            float x = rowRect.x;

            DrawLabelCell(ref x, rowRect.y, CheckWidth, string.Empty);
            DrawLabelCell(ref x, rowRect.y, ObjectWidth, string.Empty);

            Component b = entry.BakeryComponent;
            Type t = b != null ? b.GetType() : null;

            DrawLabelCell(ref x, rowRect.y, ComponentWidth, t != null ? t.Name : string.Empty);
            DrawLabelCell(ref x, rowRect.y, TypeWidth, GetBakeryLightTypeLabel(entry));
            DrawLabelCell(ref x, rowRect.y, LightModeWidth, string.Empty);
            DrawLabelCell(ref x, rowRect.y, EnabledWidth, GetBakeryEnabledText(b));
            DrawLabelCell(ref x, rowRect.y, ColorWidth, ColorToString(GetReflectedValue<Color?>(b, "color")));
            DrawLabelCell(ref x, rowRect.y, IntensityWidth, FloatToString(GetReflectedValue<float?>(b, "intensity")));
            DrawLabelCell(ref x, rowRect.y, RangeWidth, GetBakeryRangeText(b));
            DrawLabelCell(ref x, rowRect.y, SpotAngleWidth, GetBakerySpotAngleText(b));
            DrawLabelCell(ref x, rowRect.y, AreaSizeWidth, GetBakeryAreaSizeText(entry));
            DrawLabelCell(ref x, rowRect.y, CookieWidth, GetObjectName(GetReflectedObject<Object>(b, "cookie")));

            DrawRowSeparator(rowRect);
            HandleRowSelection(rowRect, entry.GameObject, excludeCheckBox: false);
        }

        // Draw the Unity Light Mode popup.
        private void DrawLightModePopup(LightEntry entry, Rect rect)
        {
            if (entry == null || entry.UnityLight == null)
            {
                EditorGUI.LabelField(rect, string.Empty);
                return;
            }

            LightmapBakeType current = entry.UnityLight.lightmapBakeType;
            int currentIndex = GetLightModeIndex(current);
            string[] labels = GetLightModeLabels();

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(rect, currentIndex, labels);

            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < LightModeValues.Length)
            {
                LightmapBakeType newType = LightModeValues[newIndex];
                if (newType != current)
                {
                    Undo.RecordObject(entry.UnityLight, "Change Light Mode");
                    entry.UnityLight.lightmapBakeType = newType;
                    EditorUtility.SetDirty(entry.UnityLight);
                    EditorSceneManager.MarkSceneDirty(entry.GameObject.scene);
                    SetMessage("LightModeChanged", entry.GameObject.name, GetLightModeLabel(newType));
                }
            }
        }

        // Get localized labels for the popup.
        private string[] GetLightModeLabels()
        {
            return new[]
            {
            GetLightModeLabel(LightmapBakeType.Realtime),
            GetLightModeLabel(LightmapBakeType.Mixed),
            GetLightModeLabel(LightmapBakeType.Baked)
        };
        }

        private string GetLightModeLabel(LightmapBakeType type)
        {
            switch (type)
            {
                case LightmapBakeType.Realtime:
                    return T("LightModeRealtime");
                case LightmapBakeType.Mixed:
                    return T("LightModeMixed");
                case LightmapBakeType.Baked:
                    return T("LightModeBaked");
                default:
                    return type.ToString();
            }
        }

        private static int GetLightModeIndex(LightmapBakeType type)
        {
            for (int i = 0; i < LightModeValues.Length; i++)
            {
                if (LightModeValues[i] == type)
                {
                    return i;
                }
            }

            return 0;
        }

        // Select the row object when the row is clicked.
        private void HandleRowSelection(Rect rowRect, GameObject go, bool excludeCheckBox)
        {
            Event e = Event.current;

            if (e.type != EventType.MouseDown || e.button != 0 || go == null)
            {
                return;
            }

            if (!rowRect.Contains(e.mousePosition))
            {
                return;
            }

            Rect checkRect = new Rect(rowRect.x, rowRect.y, CheckWidth, rowRect.height);
            if (excludeCheckBox && checkRect.Contains(e.mousePosition))
            {
                return;
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            e.Use();
        }

        private Rect NextRect(ref float x, float y, float width)
        {
            Rect rect = new Rect(x + 2, y + 2, width - 4, RowHeight - 4);
            x += width;
            return rect;
        }

        private void DrawLabelCell(ref float x, float y, float width, string text)
        {
            Rect rect = NextRect(ref x, y, width);
            EditorGUI.LabelField(rect, text ?? string.Empty);
        }

        private void DrawRowSeparator(Rect rowRect)
        {
            Rect lineRect = new Rect(rowRect.x, rowRect.yMax - 1f, rowRect.width, 1f);
            EditorGUI.DrawRect(lineRect, RowSeparatorColor);
        }

        // Build the message box style.
        private GUIStyle GetMessageBoxStyle()
        {
            if (messageBoxStyle != null)
            {
                return messageBoxStyle;
            }

            messageBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                clipping = TextClipping.Clip,
                fontSize = Mathf.Max(20, EditorStyles.helpBox.fontSize * 2)
            };

            messageBoxStyle.padding = new RectOffset(10, 10, 10, 10);
            return messageBoxStyle;
        }

        // Refresh the list from the current scenes.
        private void RefreshLightList(bool showMessage)
        {
            ResolveBakeryTypes();

            isBakeryAvailable =
                bakeryPointLightType != null ||
                bakeryDirectLightType != null ||
                bakeryLightMeshType != null;

            entries.Clear();

            if (!isBakeryAvailable)
            {
                SetMessage("BakeryNotInstalled");
                Repaint();
                return;
            }

            Light[] lights = Resources.FindObjectsOfTypeAll<Light>()
                .Where(IsSceneLight)
                .OrderBy(l => GetSceneOrder(l.gameObject.scene))
                .ThenBy(l => GetHierarchyOrderKey(l.transform), HierarchyOrderKeyComparer.Instance)
                .ToArray();

            foreach (var light in lights)
            {
                var entry = new LightEntry
                {
                    UnityLight = light,
                    GameObject = light.gameObject,
                    UnityLightType = light.type
                };

                entry.BakeryType = GetExpectedBakeryType(light.type);
                entry.IsSupported = entry.BakeryType != null;
                entry.Error = entry.IsSupported ? string.Empty : T("UnsupportedOrMissingBakery");
                entry.BakeryComponent = entry.IsSupported ? light.gameObject.GetComponent(entry.BakeryType) : null;

                entries.Add(entry);
            }

            if (showMessage)
            {
                SetMessage("Refreshed", entries.Count);
            }

            Repaint();
        }

        // Filter out non-scene and editor-only lights.
        private static bool IsSceneLight(Light light)
        {
            if (light == null || light.gameObject == null)
            {
                return false;
            }

            // Exclude assets and prefab assets.
            if (EditorUtility.IsPersistent(light))
            {
                return false;
            }

            // Exclude preview-scene objects such as inspector previews.
            if (EditorSceneManager.IsPreviewSceneObject(light) ||
                EditorSceneManager.IsPreviewSceneObject(light.gameObject))
            {
                return false;
            }

            // Exclude hidden internal editor objects.
            if (light.hideFlags != HideFlags.None || light.gameObject.hideFlags != HideFlags.None)
            {
                return false;
            }

            var scene = light.gameObject.scene;

            // Keep valid loaded scenes, including untitled scenes.
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            return true;
        }

        private static int GetSceneOrder(Scene scene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.handle == scene.handle)
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        private static List<int> GetHierarchyOrderKey(Transform tr)
        {
            var key = new List<int>();

            while (tr != null)
            {
                key.Add(tr.GetSiblingIndex());
                tr = tr.parent;
            }

            key.Reverse();
            return key;
        }

        // Find Bakery types only by name.
        private void ResolveBakeryTypes()
        {
            bakeryPointLightType = FindTypeByName("BakeryPointLight");
            bakeryDirectLightType = FindTypeByName("BakeryDirectLight");
            bakeryLightMeshType = FindTypeByName("BakeryLightMesh");
            ftLightmapsType = FindTypeByName("ftLightmaps");
        }

        private static Type FindTypeByName(string simpleOrFullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type direct = assembly.GetType(simpleOrFullName);
                if (direct != null)
                {
                    return direct;
                }

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    if (type.Name == simpleOrFullName || type.FullName == simpleOrFullName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private Type GetExpectedBakeryType(LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Point:
                case LightType.Spot:
                    return bakeryPointLightType;
                case LightType.Directional:
                    return bakeryDirectLightType;
                case LightType.Area:
                    return bakeryLightMeshType;
                default:
                    return null;
            }
        }

        // Resolve the single-add behavior from the settings.
        private RealtimeResolvedAction GetRealtimeResolvedActionForSingleAdd()
        {
            switch (realtimeAddSetting)
            {
                case RealtimeAddSetting.AutoMixed:
                    return RealtimeResolvedAction.ChangeToMixed;
                case RealtimeAddSetting.AutoBaked:
                    return RealtimeResolvedAction.ChangeToBaked;
                case RealtimeAddSetting.KeepRealtime:
                case RealtimeAddSetting.AskOnAddAll:
                default:
                    return RealtimeResolvedAction.KeepRealtime;
            }
        }

        private string GetUnityRangeText(Light light)
        {
            if (light == null)
            {
                return string.Empty;
            }

            return light.type == LightType.Point || light.type == LightType.Spot
                ? FloatToString(light.range)
                : string.Empty;
        }

        private string GetUnitySpotAngleText(Light light)
        {
            if (light == null)
            {
                return string.Empty;
            }

            return light.type == LightType.Spot ? FloatToString(light.spotAngle) : string.Empty;
        }

        private string GetUnityAreaSizeText(Light light)
        {
            if (light == null || light.type != LightType.Area)
            {
                return string.Empty;
            }

            Vector2 size = GetUnityAreaSize(light);
            return FloatToString(size.x) + " x " + FloatToString(size.y);
        }

        private string GetBakeryRangeText(Component bakery)
        {
            if (bakery == null)
            {
                return string.Empty;
            }

            float? cutoff = GetReflectedValue<float?>(bakery, "cutoff");
            if (cutoff.HasValue)
            {
                return FloatToString(cutoff.Value);
            }

            float? range = GetReflectedValue<float?>(bakery, "range");
            return FloatToString(range);
        }

        private string GetBakerySpotAngleText(Component bakery)
        {
            if (bakery == null)
            {
                return string.Empty;
            }

            float? angle = GetReflectedValue<float?>(bakery, "angle");
            if (angle.HasValue)
            {
                return FloatToString(angle.Value);
            }

            float? spotAngle = GetReflectedValue<float?>(bakery, "spotAngle");
            return FloatToString(spotAngle);
        }

        private string GetBakeryAreaSizeText(LightEntry entry)
        {
            if (entry == null || entry.UnityLight == null || entry.UnityLight.type != LightType.Area)
            {
                return string.Empty;
            }

            Vector3 scale = entry.GameObject != null ? entry.GameObject.transform.localScale : Vector3.one;
            return FloatToString(Mathf.Abs(scale.x)) + " x " + FloatToString(Mathf.Abs(scale.y));
        }

        private string GetBakeryLightTypeLabel(LightEntry entry)
        {
            if (entry == null || entry.UnityLight == null)
            {
                return string.Empty;
            }

            return entry.UnityLight.type.ToString();
        }

        private static string GetBakeryEnabledText(Component component)
        {
            if (component is Behaviour behaviour)
            {
                return behaviour.enabled.ToString();
            }

            return string.Empty;
        }

        private static string ColorToString(Color? color)
        {
            if (!color.HasValue)
            {
                return string.Empty;
            }

            Color c = color.Value;
            return $"({c.r:0.##}, {c.g:0.##}, {c.b:0.##})";
        }

        private static string FloatToString(float? value)
        {
            return value.HasValue ? value.Value.ToString("0.###") : string.Empty;
        }

        private static string GetObjectName(Object obj)
        {
            return obj != null ? obj.name : string.Empty;
        }
    }

}

#endif