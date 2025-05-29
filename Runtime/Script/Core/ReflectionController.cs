using System;
using UnityEngine;

namespace Engine.Core
{
    public static class ReflectionController
    {
        public static T CreateInstance<T>(string instanceTypeName, params object[] instanceParams)
        {
            if (string.IsNullOrWhiteSpace(instanceTypeName))
            {
                throw new ArgumentException("Type name cannot be null or empty.", nameof(instanceTypeName));
            }

            Type createType = Type.GetType(instanceTypeName);
            if (createType == null)
            {
                throw new TypeLoadException($"Type '{instanceTypeName}' could not be found.");
            }

            object createObject = null;
            try
            {
                createObject = Activator.CreateInstance(createType, instanceParams);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of type '{instanceTypeName}'.", ex);
            }

            if (createObject is T result)
            {
                return result;
            }
            else
            {
                throw new InvalidCastException($"Cannot cast object of type '{instanceTypeName}' to type '{typeof(T).FullName}'.");
            }
        }
        public static string GetNamespace<T>() => typeof(T).Namespace;
    }
}
