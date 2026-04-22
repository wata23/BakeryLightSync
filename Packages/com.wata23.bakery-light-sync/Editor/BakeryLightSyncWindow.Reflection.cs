
//License: PolyForm Noncommercial 1.0.0

//Required Notice: Copyright(c) 2026 Wata23

#if UNITY_EDITOR

namespace Wata23.BakeryLightSync.Editor
{

    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    // Reflection helpers and Bakery parameter copy logic.
    public partial class BakeryLightSyncWindow
    {
        // Copy shared light values from Unity to Bakery.
        private void ManualCopyFromUnityLight(Light src, Component dst)
        {
            if (src == null || dst == null)
            {
                return;
            }

            Type dstType = dst.GetType();
            Undo.RecordObject(dst, "Copy Unity Light Parameters To Bakery");

            if (dst is Behaviour behaviour)
            {
                behaviour.enabled = src.enabled;
            }

            Color color = src.color;
            if (src.useColorTemperature)
            {
                color *= Mathf.CorrelatedColorTemperatureToRGB(src.colorTemperature);
            }

            SetMemberIfExists(dstType, dst, "color", color);
            SetMemberIfExists(dstType, dst, "intensity", src.intensity);
            SetMemberIfExists(dstType, dst, "indirectIntensity", src.bounceIntensity);

            bool isBakeryLightMesh = bakeryLightMeshType != null && bakeryLightMeshType.IsInstanceOfType(dst);

            if (!isBakeryLightMesh)
            {
                SetMemberIfExists(dstType, dst, "cutoff", src.range);
                SetMemberIfExists(dstType, dst, "range", src.range);
                SetMemberIfExists(dstType, dst, "angle", src.spotAngle);
                SetMemberIfExists(dstType, dst, "spotAngle", src.spotAngle);
                SetMemberIfExists(dstType, dst, "cookie", src.cookie);
                SetMemberIfExists(dstType, dst, "cookieSize", GetLightFloatProperty(src, "cookieSize", 10f));
                SetMemberIfExists(dstType, dst, "shadowSpread", 0f);
                SetMemberIfExists(dstType, dst, "shadowSamples", src.shadowCustomResolution > 0 ? 64 : 16);
            }

            switch (src.type)
            {
                case LightType.Point:
                    ApplyPointLightSupplement(src, dst);
                    break;
                case LightType.Spot:
                    ApplySpotLightSupplement(src, dst);
                    break;
                case LightType.Directional:
                    ApplyDirectionalLightSupplement(src, dst);
                    break;
                case LightType.Area:
                    ApplyAreaLightSupplement(src, dst);
                    break;
            }
        }

        private void ApplyPointLightSupplement(Light src, Component dst)
        {
            Object cookie = src.cookie;
            SetMemberIfExists(dst.GetType(), dst, "cookie", cookie);
            SetEnumMemberIfExists(dst.GetType(), dst, "projMode", cookie != null ? "Cubemap" : "Omni");
        }

        private void ApplySpotLightSupplement(Light src, Component dst)
        {
            Type dstType = dst.GetType();

            SetMemberIfExists(dstType, dst, "angle", src.spotAngle);
            SetMemberIfExists(dstType, dst, "spotAngle", src.spotAngle);
            SetMemberIfExists(dstType, dst, "innerAngle", GetLightFloatProperty(src, "innerSpotAngle", 0f));
            SetMemberIfExists(dstType, dst, "sphereRadius", GetLightFloatProperty(src, "shapeRadius", 0f));

            Object cookie = src.cookie;
            if (cookie == null)
            {
                cookie = LoadDefaultSpotCookie();
            }

            SetMemberIfExists(dstType, dst, "cookie", cookie);
            SetEnumMemberIfExists(dstType, dst, "projMode", "Cookie");
        }

        private void ApplyDirectionalLightSupplement(Light src, Component dst)
        {
            float diameter = GetLightFloatProperty(src, "sunAngularDiameter", float.NaN);
            if (!float.IsNaN(diameter))
            {
                SetMemberIfExists(dst.GetType(), dst, "angle", diameter);
            }
        }

        private void ApplyAreaLightSupplement(Light src, Component dst)
        {
            Type dstType = dst.GetType();

            SetMemberIfExists(dstType, dst, "lmid", -1);
            SetMemberIfExists(dstType, dst, "bitmask", 0);
            SetMemberIfExists(dstType, dst, "bakeToIndirect", true);
            SetMemberIfExists(dstType, dst, "shadowmask", false);
            SetMemberIfExists(dstType, dst, "shadowmaskFalloff", false);
            SetMemberIfExists(dstType, dst, "maskChannel", 0);
            SetMemberIfExists(dstType, dst, "indirectIntensity", src.bounceIntensity);
            SetMemberIfExists(dstType, dst, "selfShadow", false);
            SetMemberIfExists(dstType, dst, "cutoff", 100f);
            SetMemberIfExists(dstType, dst, "samplesNear", 16);
            SetMemberIfExists(dstType, dst, "samplesFar", 256);
            SetMemberIfExists(dstType, dst, "texture", null);
        }

        // Make sure the Area cutoff is set when possible.
        private bool TryEnsureAreaLightCutoff(Component dst, float cutoff)
        {
            if (dst == null)
            {
                return false;
            }

            if (bakeryLightMeshType == null || !bakeryLightMeshType.IsInstanceOfType(dst))
            {
                return true;
            }

            SetMemberIfExists(dst.GetType(), dst, "cutoff", cutoff);

            float? reflectedCutoff = GetReflectedValue<float?>(dst, "cutoff");
            return reflectedCutoff.HasValue && Mathf.Approximately(reflectedCutoff.Value, cutoff);
        }

        // Build the required mesh setup for Area lights.
        private void EnsureAreaLightMeshSetup(Light src, Component bakeryComponent)
        {
            if (src == null || bakeryComponent == null)
            {
                return;
            }

            GameObject go = src.gameObject;

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = Undo.AddComponent<MeshFilter>(go);
            }
            else
            {
                Undo.RecordObject(meshFilter, "Set Area Light MeshFilter");
            }

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = Undo.AddComponent<MeshRenderer>(go);
            }
            else
            {
                Undo.RecordObject(meshRenderer, "Set Area Light MeshRenderer");
            }

            if (meshFilter.sharedMesh == null || meshFilter.sharedMesh.name != "Quad")
            {
                GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                try
                {
                    Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
                    meshFilter.sharedMesh = quadMesh;
                }
                finally
                {
                    Object.DestroyImmediate(tempQuad);
                }
            }

            Material mat = LoadDefaultAreaLightMaterial();
            if (meshRenderer.sharedMaterial == null && mat != null)
            {
                meshRenderer.sharedMaterial = mat;
            }

            Vector2 areaSize = GetUnityAreaSize(src);

            Undo.RecordObject(go.transform, "Set Area Light Transform Scale");
            go.transform.localScale = new Vector3(Mathf.Abs(areaSize.x), Mathf.Abs(areaSize.y), 1f);

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(meshRenderer);
            EditorUtility.SetDirty(go.transform);
        }

        private Texture2D LoadDefaultSpotCookie()
        {
            if (defaultSpotCookie != null)
            {
                return defaultSpotCookie;
            }

            string runtimePath = GetBakeryRuntimePath();
            if (string.IsNullOrEmpty(runtimePath))
            {
                return null;
            }

            defaultSpotCookie = AssetDatabase.LoadAssetAtPath<Texture2D>(runtimePath + "ftUnitySpotTexture.bmp");
            return defaultSpotCookie;
        }

        private Material LoadDefaultAreaLightMaterial()
        {
            if (defaultAreaMaterial != null)
            {
                return defaultAreaMaterial;
            }

            string runtimePath = GetBakeryRuntimePath();
            if (string.IsNullOrEmpty(runtimePath))
            {
                return null;
            }

            defaultAreaMaterial = AssetDatabase.LoadAssetAtPath<Material>(runtimePath + "ftDefaultAreaLightMat.mat");
            return defaultAreaMaterial;
        }

        // Ask Bakery for its runtime asset folder.
        private string GetBakeryRuntimePath()
        {
            if (ftLightmapsType == null)
            {
                ftLightmapsType = FindTypeByName("ftLightmaps");
            }

            if (ftLightmapsType == null)
            {
                return string.Empty;
            }

            MethodInfo method = ftLightmapsType.GetMethod("GetRuntimePath", StaticFlags);
            if (method == null)
            {
                return string.Empty;
            }

            try
            {
                object result = method.Invoke(null, null);
                return result as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Set a field or property when it exists.
        private static void SetMemberIfExists(Type type, object target, string name, object value)
        {
            if (type == null || target == null || string.IsNullOrEmpty(name))
            {
                return;
            }

            FieldInfo field = type.GetField(name, InstanceFlags);
            if (field != null)
            {
                TrySetValue(field.FieldType, v => field.SetValue(target, v), value);
                return;
            }

            PropertyInfo property = type.GetProperty(name, InstanceFlags);
            if (property != null && property.CanWrite)
            {
                TrySetValue(property.PropertyType, v => property.SetValue(target, v, null), value);
            }
        }

        // Set an enum field or property when it exists.
        private static void SetEnumMemberIfExists(Type type, object target, string name, string enumName)
        {
            if (type == null || target == null || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(enumName))
            {
                return;
            }

            FieldInfo field = type.GetField(name, InstanceFlags);
            if (field != null && field.FieldType.IsEnum)
            {
                TrySetEnum(field.FieldType, enumName, value => field.SetValue(target, value));
                return;
            }

            PropertyInfo property = type.GetProperty(name, InstanceFlags);
            if (property != null && property.CanWrite && property.PropertyType.IsEnum)
            {
                TrySetEnum(property.PropertyType, enumName, value => property.SetValue(target, value, null));
            }
        }

        private static void TrySetEnum(Type enumType, string enumName, Action<object> setter)
        {
            try
            {
                object enumValue = Enum.Parse(enumType, enumName, true);
                setter(enumValue);
            }
            catch
            {
                // Ignore enum mismatches across Bakery versions.
            }
        }

        private static void TrySetValue(Type destinationType, Action<object> setter, object value)
        {
            try
            {
                if (value == null)
                {
                    if (!destinationType.IsValueType || Nullable.GetUnderlyingType(destinationType) != null)
                    {
                        setter(null);
                    }
                    return;
                }

                Type valueType = value.GetType();
                if (destinationType.IsAssignableFrom(valueType))
                {
                    setter(value);
                    return;
                }

                if (destinationType == typeof(float) && value is int intValue)
                {
                    setter((float)intValue);
                    return;
                }

                if (destinationType == typeof(int) && value is float floatValue)
                {
                    setter(Mathf.RoundToInt(floatValue));
                    return;
                }

                if (typeof(Object).IsAssignableFrom(destinationType) && value is Object unityObject)
                {
                    if (destinationType.IsInstanceOfType(unityObject))
                    {
                        setter(unityObject);
                    }
                }
            }
            catch
            {
                // Ignore unsupported assignments across Bakery versions.
            }
        }

        // Read a reflected value when it exists.
        private static T GetReflectedValue<T>(Component component, string memberName)
        {
            if (component == null)
            {
                return default;
            }

            object value = GetReflectedObject(component, memberName);

            if (value == null)
            {
                return default;
            }

            Type targetType = typeof(T);
            Type nullableInnerType = Nullable.GetUnderlyingType(targetType);

            try
            {
                if (nullableInnerType != null)
                {
                    if (nullableInnerType.IsInstanceOfType(value))
                    {
                        return (T)value;
                    }

                    return default;
                }

                if (value is T typed)
                {
                    return typed;
                }
            }
            catch
            {
                return default;
            }

            return default;
        }

        private static T GetReflectedObject<T>(Component component, string memberName) where T : Object
        {
            object value = GetReflectedObject(component, memberName);
            return value as T;
        }

        private static object GetReflectedObject(Component component, string memberName)
        {
            if (component == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            Type type = component.GetType();

            FieldInfo field = type.GetField(memberName, InstanceFlags);
            if (field != null)
            {
                return field.GetValue(component);
            }

            PropertyInfo property = type.GetProperty(memberName, InstanceFlags);
            if (property != null && property.CanRead)
            {
                return property.GetValue(component, null);
            }

            return null;
        }

        private static float GetLightFloatProperty(Light light, string propertyName, float fallback)
        {
            if (light == null)
            {
                return fallback;
            }

            PropertyInfo prop = typeof(Light).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || !prop.CanRead)
            {
                return fallback;
            }

            try
            {
                object value = prop.GetValue(light, null);
                return value is float f ? f : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static Vector2 GetUnityAreaSize(Light light)
        {
            if (light == null)
            {
                return Vector2.one;
            }

            PropertyInfo prop = typeof(Light).GetProperty("areaSize", BindingFlags.Instance | BindingFlags.Public);
            if (prop == null || !prop.CanRead)
            {
                return Vector2.one;
            }

            try
            {
                object value = prop.GetValue(light, null);
                return value is Vector2 v ? v : Vector2.one;
            }
            catch
            {
                return Vector2.one;
            }
        }
    }

}

#endif