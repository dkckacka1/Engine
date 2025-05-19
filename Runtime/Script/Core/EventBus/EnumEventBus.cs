using System;
using System.Collections.Generic;
using Engine.Util.Extension;
using UnityEngine.Events;

namespace Engine.Core.EventBus
{
    public class EnumEventBus<T> : IDisposable where T : System.Enum
    {
        private Dictionary<T, UnityEvent> eventDic = new();
        private T currentype = default;

        public T CurrentType => currentype;

        public EnumEventBus()
        {
            EnumExtension.Foreach<T>((type) =>
            {
                eventDic.Add(type, new UnityEvent());
            });
        }

        public void ChangeEvent(T type)
        {
            currentype = type;
            InvokeEvent(CurrentType);
        }

        public void PublishEvent(T eventType, UnityAction action)
        {
            eventDic[eventType].AddListener(action);
        }

        public void RemoveEvent(T eventType, UnityAction action)
        {
            eventDic[eventType].RemoveListener(action);
        }

        public void ClearEvent(T eventType)
        {
            eventDic[eventType].RemoveAllListeners();
        }

        private void InvokeEvent(T eventType)
        {
            eventDic[eventType]?.Invoke();
        }

        public void Dispose()
        {
            EnumExtension.Foreach<T>((type) =>
            {
                ClearEvent(type);
            });

            eventDic.Clear();
        }
    }
}
