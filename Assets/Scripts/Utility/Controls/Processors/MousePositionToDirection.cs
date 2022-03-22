using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Controls.Processors
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class CursorPositionToManhattanDirection : InputProcessor<Vector2>
    {
#if UNITY_EDITOR
        static CursorPositionToManhattanDirection()
        {
            Initialise();
        }
#endif
        public override Vector2 Process(Vector2 value, InputControl control)
        {
            value /= new Vector2(Screen.width, Screen.height);
            value -= new Vector2(0.5f, 0.5f);
            value *= 2;
            return value;
        }

        [RuntimeInitializeOnLoadMethod]
        static void Initialise()
        {
            InputSystem.RegisterProcessor<CursorPositionToManhattanDirection>();
        }
    }

}