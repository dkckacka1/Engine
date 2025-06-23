using System;
using System.Collections.Generic;
using Engine.Util.Extension;
using UnityEngine.Events;

namespace Engine.Core.EventBus
{
    public class EnumEventBus<T> : IDisposable where T : Enum
    {
        private readonly UnityEvent<T> _enumEvent = new();

        public T CurrentType { get; private set; }
        
        public EnumEventBus(T defaultType)
        {
            CurrentType = defaultType;
        }

        public bool IsSameCurrentType(T type)
        {
            return CurrentType.Equals(type);
        }

        public bool IsSameCurrentType(params T[] types)
        {
            foreach (var type in types)
            {
                if (CurrentType.Equals(type))
                    return true;
            }

            return false;
        }
        
        public void ChangeEvent(T type)
        {
            CurrentType = type;
            InvokeEvent(CurrentType);
        }

        public void PublishEvent(IEnumEvent<T> enumEvent)
        {
            _enumEvent.AddListener(enumEvent.OnEnumChange);
        }

        public void UnPublishEvent(IEnumEvent<T> enumEvent)
        {
            _enumEvent.RemoveListener(enumEvent.OnEnumChange);
        }

        public void ClearEvent()
        {
            _enumEvent.RemoveAllListeners();
        }

        private void InvokeEvent(T eventType)
        {
            _enumEvent.Invoke(eventType);
        }

        public void Dispose()
        {
            ClearEvent();
        }
    }
}
