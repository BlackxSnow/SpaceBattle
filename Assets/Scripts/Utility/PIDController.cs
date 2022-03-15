using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UILineRenderer;

namespace Utility
{
    [Serializable]
    public class PIDController
    {
        public float ProportionalGain;
        public float IntegralGain;
        public float DerivativeGain;

        private float LastError;
        private float ErrorIntegral;

        public UILineRenderer.UILine DebugLine;


        public float PID(float error)
        {
            ErrorIntegral += error * Time.deltaTime;
            float derivativeError = (error - LastError) / Time.deltaTime;
            LastError = error;

            Debug.Log($"(PID) PGain: {ProportionalGain} | IGain: {IntegralGain} | DGain: {DerivativeGain} | Error: {error} | IError: {ErrorIntegral} | DError: {derivativeError}");

            float result = ProportionalGain * error + IntegralGain * ErrorIntegral + DerivativeGain * derivativeError;

            if (DebugLine)
            {
                for (int i = 1; i < DebugLine.BezierControlPoints.Length; i++)
                {
                    BezierPoint current = DebugLine.BezierControlPoints[i];
                    BezierPoint last = DebugLine.BezierControlPoints[i - 1];
                    current.Position.x = i;
                    last.Position = current.Position;

                }
                DebugLine.BezierControlPoints[DebugLine.BezierControlPoints.Length - 1].Position.x = DebugLine.BezierControlPoints.Length;
                DebugLine.BezierControlPoints[DebugLine.BezierControlPoints.Length - 1].Position.y = error;
                DebugLine.SetAllDirty();
            }

            return result;
        }
        public float PI(float error)
        {
            ErrorIntegral += error * Time.deltaTime;
            LastError = error;

            return ProportionalGain * error + IntegralGain * ErrorIntegral;
        }

        public void Reset()
        {
            ErrorIntegral = 0;
            LastError = 0;
        }

        public void SetGain(float p, float i, float d)
        {
            ProportionalGain = p;
            IntegralGain = i;
            DerivativeGain = d;
        }

        public PIDController(float proportionalGain, float integralGain, float derivativeGain, float initialError, UILineRenderer.UILine debugLine = null)
        {
            LastError = initialError;
            ProportionalGain = proportionalGain;
            IntegralGain = integralGain;
            DerivativeGain = derivativeGain;
            DebugLine = debugLine;
        }
    }
}
