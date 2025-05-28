using Cysharp.Threading.Tasks;
using Engine.Util;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Engine.Core.Time
{
    public class TimeManager : SingletonMonoBehaviour<TimeManager>
    {
        private class TimeValue
        {
            public float TimeFactor = 1f;
            public readonly UnityEvent<float> FactorChangeEvent = new();
            public bool TimerPause = false;

            public TimeValue(float timeFactor = DEFAULT_TIME_SCALE_FACTOR)
            {
                TimeFactor = timeFactor;
            }

            public static implicit operator float(TimeValue timeValue) => timeValue.TimeFactor;

            public static implicit operator UnityEvent<float>(TimeValue timeValue) => timeValue.FactorChangeEvent;
        }

        private readonly Dictionary<string, TimeValue> _timeScaleDic = new();

        private const string DEFAULT_TIME_KEY = "Default";
        private const float DEFAULT_TIME_SCALE_FACTOR = 1f;

        public float GetTimeScaleFactor(string timeKey = DEFAULT_TIME_KEY)
        {
            if (_timeScaleDic.ContainsKey(timeKey) is false)
            {
                _timeScaleDic.Add(timeKey, new TimeValue());
            }
            
            return _timeScaleDic[timeKey];
        }
        
        public float GetDeltaTimeScale(string timeKey = DEFAULT_TIME_KEY)
        {
            if (_timeScaleDic.ContainsKey(timeKey) is false)
            {
                _timeScaleDic.Add(timeKey, new TimeValue());
            }
            
            return _timeScaleDic[timeKey] * UnityEngine.Time.deltaTime;
        }

        public UnityEvent<float> GetFactorChangeEvent(string timeKey)
        {
            if (_timeScaleDic.ContainsKey(timeKey) is false)
            {
                _timeScaleDic.Add(timeKey, new TimeValue());
            }

            return _timeScaleDic[timeKey];
        }

        public void SetTimeScaleFactor(string timeKey, float timeScale)
        {
            if (_timeScaleDic.ContainsKey(timeKey) is false)
            {
                _timeScaleDic.Add(timeKey, new TimeValue(timeScale));
            }

            _timeScaleDic[timeKey].TimeFactor = timeScale;
            _timeScaleDic[timeKey].FactorChangeEvent?.Invoke(timeScale);
        }

        public void SettingTimer(string timeKey, bool isPause)
        {
            _timeScaleDic[timeKey].TimerPause = isPause;
        }
        
        public async UniTask StartTimer(float waitTime, string timeKey = DEFAULT_TIME_KEY, UnityAction<float> timerAction = null)
        {
            var timer = waitTime;

            while (timer > 0)
            {
                await UniTask.NextFrame();
                if (_timeScaleDic[timeKey].TimerPause)
                    continue;
                
                timerAction?.Invoke(timer);
                timer -= GetDeltaTimeScale(timeKey);
            }
        }

        private void ResetAllTimeScale()
        {
            foreach (var timeKey in _timeScaleDic.Keys)
            {
                SetTimeScaleFactor(timeKey, DEFAULT_TIME_SCALE_FACTOR);
            }
        }


    }
}