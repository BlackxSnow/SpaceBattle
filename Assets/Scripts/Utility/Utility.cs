using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

public static class Util
{
    [Serializable]
    public struct Bounds1D
    {
        public float Min;
        public float Max;

        public Bounds1D(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
    public static bool WithinBounds(float f, Bounds1D bounds, bool inclusive = false)
    {
        if (!inclusive)
        {
            return f > bounds.Min && f < bounds.Max ? true : false; 
        }
        else
        {
            return f >= bounds.Min && f <= bounds.Max ? true : false;
        }
    }
    public static Quaternion FromTo(Quaternion a, Quaternion b)
    {
        return b * Quaternion.Inverse(a);
    }
    public static Vector3 ToVector3(this float f) 
    { 
        return new Vector3(f, f, f); 
    }
    public static Vector3Int Abs(this Vector3Int v3)
    {
        return new Vector3Int(Mathf.Abs(v3.x), Mathf.Abs(v3.y), Mathf.Abs(v3.z));
    }
    public static Vector3 UNormalized(this Vector3 v3)
    {
        return v3 / Mathf.Sqrt(Mathf.Pow(v3.x, 2) + Mathf.Pow(v3.y, 2) + Mathf.Pow(v3.z, 2));
    }
    public static Vector2 UNormalized(this Vector2 v2)
    {
        return v2 / Mathf.Sqrt(Mathf.Pow(v2.x, 2) + Mathf.Pow(v2.y, 2));
    }

    public static Vector2 Perpendicular(this Vector2 a)
    {
        Vector2 b = new Vector2(0, a.x);
        b.x = -(a.y * b.y) / a.x;
        b.UNormalized();
        b *= a.magnitude;
        return b;
    }

    public static Vector3 Perpendicular(Vector3 a, Vector3 b)
    {
        return Vector3.Cross(a, b);
    }

    public static float Map(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    public static bool TryGetComponentInParent<T>(this Component origin, out T component)
    {
        component = default(T);
        try
        {
            component = origin.GetComponentInParent<T>();
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }

    public static Transform[] GetChildren(this Transform transform)
    {
        Transform[] result = new Transform[transform.childCount];
        for(int i = 0; i < transform.childCount; i++)
        {
            result[i] = transform.GetChild(i);
        }
        return result;
    }

    public static List<Transform> FindChildren(this Transform transform, string name, bool treeSearch = false)
    {
        Transform[] children = transform.GetChildren();
        List<Transform> result = new List<Transform>();
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Contains(name))
            {
                result.Add(children[i]);
            }
            if (treeSearch)
            {
                Transform[] subChildren = children[i].GetChildren();
                if (subChildren.Length > 0)
                {
                    result.AddRange(children[i].FindChildren(name, true));
                } 
            }
        }
        return result;
    }
}
