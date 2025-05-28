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
            public float timeFactor = 1f;
            public UnityEvent<float> FactorChangeEvent = new();

            public TimeValue(float timeFactor = DefaultTimeScaleFactor)
            {
                timeFactor = DefaultTimeScaleFactor;
            }

            public static implicit operator float(TimeValue timeValue) => timeValue.timeFactor;

            public static implicit operator UnityEvent<float>(TimeValue timeValue) => timeValue.FactorChangeEvent;
        }

        private Dictionary<string, TimeValue> timeScaleDic = new();

        private const string DefaultTimeKey = "Default";
        private const float DefaultTimeScaleFactor = 1f;

        public float GetTimeScale(string timeKey = DefaultTimeKey)
        {
            if (timeScaleDic.ContainsKey(timeKey) is false)
            {
                timeScaleDic.Add(timeKey, new());
            }

            return timeScaleDic[timeKey];
        }

        public void SetTimeScale(string timeKey, float timeScale)
        {
            if (timeScaleDic.ContainsKey(timeKey) is false)
            {
                timeScaleDic.Add(timeKey, new(timeScale));
            }

            timeScaleDic[timeKey].timeFactor = timeScale;
            timeScaleDic[timeKey].FactorChangeEvent?.Invoke(timeScale);
        }

        private void ResetAllTimeScale()
        {
            foreach (var timeKey in timeScaleDic.Keys)
            {
                SetTimeScale(timeKey, DefaultTimeScaleFactor);
            }
        }

        private async UniTask WaitSecondTime(float waitTime, string timeKey = DefaultTimeKey)
        {
            float timer = waitTime;

            while (timer <= 0)
            {
                await UniTask.WaitForEndOfFrame();
                timer -= UnityEngine.Time.deltaTime * timeScaleDic[timeKey];
            }
        }
    }
}