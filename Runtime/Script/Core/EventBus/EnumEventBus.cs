using System;
using System.Collections.Generic;
using Engine.Util.Extension;
using UnityEngine.Events;

namespace Engine.Core.EventBus
{
    public class EnumEventBus<T> : IDisposable where T : Enum
    {
        private readonly UnityEvent<T> _enumEvent = new();
        private T _currentType = default;

        public T CurrentType => _currentType;

        public void ChangeEvent(T type)
        {
            _currentType = type;
            InvokeEvent(CurrentType);
        }

        public void PublishEvent(IEnumEvent<T> enumEvent)
        {
            _enumEvent.AddListener(enumEvent.OnEnumChange);
        }

        public void RemoveEvent(IEnumEvent<T> enumEvent)
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
