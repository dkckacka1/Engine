using System.Linq;
using UnityEngine;

namespace Engine.Util.Extension
{
    public static class TransformExtension
    {
        public static Transform GetChildByName(this Transform transform, string name)
        {
            return transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals(name));
        }
    }
}