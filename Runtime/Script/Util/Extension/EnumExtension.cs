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
    }
}