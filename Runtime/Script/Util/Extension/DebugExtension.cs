
using UnityEngine;

namespace Engine.Util.Extension
{
    public static class DebugExtension
    {
        public static void PrintComponent(Transform targetObj)
        {
            string print = string.Empty;

            print += $"{targetObj.name}'s Component\n";

            foreach (Component component in targetObj.GetComponents<Component>())
            {
                print += $"{component.GetType().FullName}\n";
            }

            Debug.Log(print, targetObj);
        }
    }
}