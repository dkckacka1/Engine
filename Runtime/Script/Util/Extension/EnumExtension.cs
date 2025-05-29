using UnityEngine;

namespace Engine.Util.Extension
{
    public static class EnumExtension
    {
        public static void Foreach<T>(System.Action<T> action) where T : System.Enum
        {
            foreach(var type in System.Enum.GetValues(typeof(T)))
            {
                action?.Invoke((T)type);
            }
        }

        public static int GetEnumCount<T>() where T : System.Enum
        {
            return System.Enum.GetValues(typeof(T)).Length;
        }
        
        public static T GetMoveNext<T>(this T source) where T : System.Enum
        {
            var array = System.Enum.GetValues(typeof(T));
            for (int i = 0; i < array.Length - 1; i++)
            {
                if (source.Equals(array.GetValue(i)))
                    return (T)array.GetValue(i + 1);
            }
            return (T)array.GetValue(0);
        }
    }
}