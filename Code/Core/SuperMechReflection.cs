using System;
using System.Reflection;
using UnityEngine;

namespace SuperMech.Code
{
    public static class SuperMechReflection
    {
        private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags All = AllInstance | AllStatic;

        private static readonly System.Collections.Generic.Dictionary<string, FieldInfo> _fieldCache =
            new System.Collections.Generic.Dictionary<string, FieldInfo>();
        private static readonly System.Collections.Generic.Dictionary<string, MethodInfo> _methodCache =
            new System.Collections.Generic.Dictionary<string, MethodInfo>();
        private static readonly System.Collections.Generic.Dictionary<string, PropertyInfo> _propertyCache =
            new System.Collections.Generic.Dictionary<string, PropertyInfo>();

        private static string CacheKey(Type type, string name, BindingFlags flags)
        {
            return $"{type.FullName}.{name}.{(int)flags}";
        }

        public static void ClearCache()
        {
            _fieldCache.Clear();
            _methodCache.Clear();
            _propertyCache.Clear();
        }

        public static T GetFieldValue<T>(object obj, string fieldName, BindingFlags flags = AllInstance)
        {
            if (obj == null) return default;
            try
            {
                FieldInfo field = GetCachedField(obj.GetType(), fieldName, flags);
                if (field == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射字段未找到: {obj.GetType().Name}.{fieldName}");
                    return default;
                }
                return (T)field.GetValue(obj);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射获取字段失败 {obj.GetType().Name}.{fieldName}: {e.Message}");
                return default;
            }
        }

        private static FieldInfo GetCachedField(Type type, string fieldName, BindingFlags flags)
        {
            string key = CacheKey(type, fieldName, flags);
            if (_fieldCache.TryGetValue(key, out FieldInfo cached)) return cached;
            FieldInfo field = type.GetField(fieldName, flags);
            _fieldCache[key] = field;
            return field;
        }

        public static T GetStaticFieldValue<T>(Type type, string fieldName)
        {
            if (type == null) return default;
            try
            {
                FieldInfo field = GetCachedField(type, fieldName, AllStatic);
                if (field == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射静态字段未找到: {type.Name}.{fieldName}");
                    return default;
                }
                return (T)field.GetValue(null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射获取静态字段失败 {type.Name}.{fieldName}: {e.Message}");
                return default;
            }
        }

        public static void SetFieldValue(object obj, string fieldName, object value, BindingFlags flags = AllInstance)
        {
            if (obj == null) return;
            try
            {
                FieldInfo field = GetCachedField(obj.GetType(), fieldName, flags);
                if (field == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射字段未找到: {obj.GetType().Name}.{fieldName}");
                    return;
                }
                field.SetValue(obj, value);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射设置字段失败 {obj.GetType().Name}.{fieldName}: {e.Message}");
            }
        }

        public static void SetStaticFieldValue(Type type, string fieldName, object value)
        {
            if (type == null) return;
            try
            {
                FieldInfo field = GetCachedField(type, fieldName, AllStatic);
                if (field == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射静态字段未找到: {type.Name}.{fieldName}");
                    return;
                }
                field.SetValue(null, value);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射设置静态字段失败 {type.Name}.{fieldName}: {e.Message}");
            }
        }

        public static T GetPropertyValue<T>(object obj, string propertyName, BindingFlags flags = AllInstance)
        {
            if (obj == null) return default;
            try
            {
                PropertyInfo prop = GetCachedProperty(obj.GetType(), propertyName, flags);
                if (prop == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射属性未找到: {obj.GetType().Name}.{propertyName}");
                    return default;
                }
                return (T)prop.GetValue(obj);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射获取属性失败 {obj.GetType().Name}.{propertyName}: {e.Message}");
                return default;
            }
        }

        private static PropertyInfo GetCachedProperty(Type type, string propertyName, BindingFlags flags)
        {
            string key = CacheKey(type, propertyName, flags);
            if (_propertyCache.TryGetValue(key, out PropertyInfo cached)) return cached;
            PropertyInfo prop = type.GetProperty(propertyName, flags);
            _propertyCache[key] = prop;
            return prop;
        }

        public static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags = All)
        {
            if (type == null) return null;
            try
            {
                return GetCachedMethod(type, methodName, flags);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射获取方法失败 {type.Name}.{methodName}: {e.Message}");
                return null;
            }
        }

        private static MethodInfo GetCachedMethod(Type type, string methodName, BindingFlags flags)
        {
            string key = CacheKey(type, methodName, flags);
            if (_methodCache.TryGetValue(key, out MethodInfo cached)) return cached;
            MethodInfo method = type.GetMethod(methodName, flags);
            _methodCache[key] = method;
            return method;
        }

        public static object InvokeMethod(object obj, string methodName, object[] args = null, BindingFlags flags = AllInstance)
        {
            if (obj == null) return null;
            try
            {
                MethodInfo method = GetCachedMethod(obj.GetType(), methodName, flags);
                if (method == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射方法未找到: {obj.GetType().Name}.{methodName}");
                    return null;
                }
                return method.Invoke(obj, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射调用方法失败 {obj.GetType().Name}.{methodName}: {e.Message}");
                return null;
            }
        }

        public static object InvokeStaticMethod(Type type, string methodName, object[] args = null)
        {
            if (type == null) return null;
            try
            {
                MethodInfo method = GetCachedMethod(type, methodName, AllStatic);
                if (method == null)
                {
                    Debug.LogWarning($"[超神机械师] 反射静态方法未找到: {type.Name}.{methodName}");
                    return null;
                }
                return method.Invoke(null, args);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 反射调用静态方法失败 {type.Name}.{methodName}: {e.Message}");
                return null;
            }
        }

        public static T LoadWindowPrefab<T>(string windowId) where T : Component
        {
            try
            {
                MethodInfo method = GetCachedMethod(typeof(WindowPreloader), "getWindowPrefab", AllStatic);
                if (method == null)
                {
                    Debug.LogWarning($"[超神机械师] WindowPreloader.getWindowPrefab方法未找到");
                    return null;
                }
                return method.Invoke(null, new object[] { windowId }) as T;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[超神机械师] 加载窗口预制体失败 {windowId}: {e.Message}");
                return null;
            }
        }
    }
}
