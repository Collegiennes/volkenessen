using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

class BulletTimeManager : MonoBehaviour, IBulletTimeManager
{
    static BulletTimeManager instance;
    public static IBulletTimeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(BulletTimeManager)) as BulletTimeManager;
                if (instance == null)
                    throw new InvalidOperationException("No instance in scene!");
            }
            return instance;
        }
    }
    void OnApplicationQuit()
    {
        instance = null;
    }

    const float BulletTimeFactor = 0.1f;
    const float BulletTimeDuration = 0.4f;

    float MaximumSlowdown;
    public float TotalDuration { get; set; }

    readonly Stopwatch sw = new Stopwatch();
    

    void FixedUpdate()
    {
        if (!sw.IsRunning)
        {
            Time.timeScale = 1;
            return;
        }
        if (sw.Elapsed.TotalSeconds > TotalDuration)
        {
            sw.Stop();
            Time.timeScale = 1;
            return;
        }

        var elapsed = (float) sw.Elapsed.TotalSeconds;

        var step = Easing.EaseIn(Easing.EaseOut(Mathf.Clamp01(elapsed / TotalDuration), EasingType.Quadratic), EasingType.Quintic);
        var slowFactor = Mathf.Lerp(MaximumSlowdown, 1, step);

        Time.timeScale = slowFactor;
        Time.fixedDeltaTime = 0.02f * slowFactor;
        Time.maximumDeltaTime = 1 / 3f * slowFactor;

        Camera.main.audio.pitch = Mathf.Lerp(Time.timeScale, 1, 0.75f);
    }

    public void AddBulletTime(float power)
    {
        // Time left to last shot?
        var timeLeftFactor = sw.IsRunning ? 1 - (float)sw.Elapsed.TotalSeconds / TotalDuration : 0;
        //Debug.Log("Power : " + power + " | Time left factor : " + timeLeftFactor);

        if (!sw.IsRunning)
        {
            sw.Reset();
            sw.Start();
            MaximumSlowdown = float.MaxValue;
            TotalDuration = 0;
        }

        MaximumSlowdown = Math.Min(MaximumSlowdown, Mathf.Lerp(1, BulletTimeFactor, Mathf.Clamp01(power + timeLeftFactor)));
        TotalDuration = TotalDuration * timeLeftFactor + power * BulletTimeDuration;
        //Debug.Log("New max slowdown : " + MaximumSlowdown + " | New total duration : " + TotalDuration);
    }
}

public interface IBulletTimeManager
{
    void AddBulletTime(float power);
    float TotalDuration { get; }
}
