using Cysharp.Threading.Tasks;
using Engine.Util;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Engine.Core.Time
{
    public class TimeManager : SingletonMonoBehaviour<TimeManager>
    {
        private class TimeValue
        {
            public float TimeFactor = 1f;
            public readonly UnityEvent<float> FactorChangeEvent = new();

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

        public float GetTimeScale(string timeKey = DEFAULT_TIME_KEY)
        {
            if (_timeScaleDic.ContainsKey(timeKey) is false)
            {
                _timeScaleDic.Add(timeKey, new TimeValue());
            }

            return _timeScaleDic[timeKey];
        }

        public void SetTimeScale(string timeKey, float timeScale)
        {
            if (_timeScaleDic.ContainsKey(timeKey) is false)
            {
                _timeScaleDic.Add(timeKey, new TimeValue(timeScale));
            }

            _timeScaleDic[timeKey].TimeFactor = timeScale;
            _timeScaleDic[timeKey].FactorChangeEvent?.Invoke(timeScale);
        }

        private void ResetAllTimeScale()
        {
            foreach (var timeKey in _timeScaleDic.Keys)
            {
                SetTimeScale(timeKey, DEFAULT_TIME_SCALE_FACTOR);
            }
        }

        private async UniTask WaitSecondTime(float waitTime, string timeKey = DEFAULT_TIME_KEY)
        {
            var timer = waitTime;

            while (timer <= 0)
            {
                await UniTask.WaitForEndOfFrame();
                timer -= UnityEngine.Time.deltaTime * _timeScaleDic[timeKey];
            }
        }
    }
}