using System.Collections.Generic;
using UnityEngine;

namespace Engine.EditorUtil
{
    [System.Serializable]
    public struct GitData
    {
        public string GitName;
        public string GitVersion;
        public string GitURL;
    }

    public class DependenceData : ScriptableObject
    {
        public List<GitData> gitDataList;
    }
}