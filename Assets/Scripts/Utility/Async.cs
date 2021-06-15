using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Nito.AsyncEx;
using UnityAsync;
using System.Threading;

namespace Utility
{
    public static class Async
    {

        public class Timer
        {
            //Limit in seconds
            public float Limit = 1f;
            public float Current = 0f;
            private float Interval;
            private float LastTime;
            public bool Repeat;

            public delegate void ElapsedDelegate(float ActualTime);
            public ElapsedDelegate Elapsed;
            public TaskCompletionSource<bool> CompletionSource;
            private CancellationTokenSource TokenSource;

            public bool Enabled = false;

            private float StartTime;
            public void Start()
            {
                Current = 0f;
                Interval = Mathf.Min(Limit / 5f, 0.01f);
                Enabled = true;
                LastTime = Time.time;
                StartTime = Time.time;
                TokenSource = new CancellationTokenSource();
                CompletionSource = new TaskCompletionSource<bool>();
                Counter();
            }
            public void Stop()
            {
                Enabled = false;
                Current = 0f;
                TokenSource?.Cancel();
            }

            async void Counter()
            {
                while (Enabled)
                {
                    
                    bool passed = await WaitForSeconds(Limit, TokenSource.Token);
                    if (!passed) return;

                    CompletionSource.SetResult(true);
                    Elapsed(Time.time - StartTime);
                    StartTime = Time.time;
                    if (!Repeat) break;
                }
            }

            public Timer(float Seconds, ElapsedDelegate RunMethod, bool Repeat = false)
            {
                this.Repeat = Repeat;
                Limit = Seconds;
                Elapsed = RunMethod;
            }


        }

        public static async Task<bool> WaitForSeconds(float time, CancellationToken token, IProgress<float> progress = null)
        {
            float currentTime = 0;
            while (currentTime < time)
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                currentTime += Time.deltaTime;
                progress?.Report(currentTime / time);
                await Await.NextUpdate();
            }
            return true;
        }
    }

}