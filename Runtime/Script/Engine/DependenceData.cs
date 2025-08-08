using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Engine.EditorUtil
{
    [System.Serializable]
    public struct GitData
    {
        public string name;
        public string gitName;
        public string gitVersion;
        public string gitURL;
    }

    public class DependenceData : ScriptableObject
    {
        public List<GitData> gitDataList;
    }
}