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
    public static class Collections
    {
        public static IEnumerable<Enum> GetFlags(Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static T[] CombineArrays<T>(params T[][] Arrays)
        {
            int ArrayLength = 0;
            for (int i = 0; i < Arrays.Length; i++)
            {
                ArrayLength += Arrays[i].Length;
            }
            T[] ReturnArray = new T[ArrayLength];
            int CurrentIndex = 0;
            for (int i = 0; i < Arrays.Length; i++)
            {
                Array.Copy(Arrays[i], 0, ReturnArray, CurrentIndex, Arrays[i].Length);
                CurrentIndex += Arrays[i].Length;
            }
            return ReturnArray;
        }

        public static List<T> CombineLists<T>(List<T> BaseList, params List<T>[] AdditionalLists)
        {
            for (int i = 0; i < AdditionalLists.Length; i++)
            {
                foreach (T item in AdditionalLists[i])
                {
                    BaseList.Add(item);
                }
            }
            return BaseList;
        }

        public static Dictionary<T, V> DeserializeEnumCollection<T, V>(Dictionary<string, V> Input)
        {

            Dictionary<T, V> Result = new Dictionary<T, V>();
            if (Input == null) return null;
            foreach (KeyValuePair<string, V> kvp in Input)
            {
                T ParsedValue = (T)Enum.Parse(typeof(T), kvp.Key);
                Result.Add(ParsedValue, kvp.Value);
            }
            return Result;
        }
        public static List<T> DeserializeEnumCollection<T>(List<string> Input)
        {
            List<T> Result = new List<T>();
            if (Input == null) return null;
            foreach (string TypeString in Input)
            {
                T ParsedValue = (T)Enum.Parse(typeof(T), TypeString);
                Result.Add(ParsedValue);
            }
            return Result;
        }


        public async static Task QueueRemovalFromList<T>(List<T> TargetList, T ToRemove, AsyncManualResetEvent RunQueue, object LockObject)
        {
            await RunQueue.WaitAsync();
            lock (LockObject)
            {
                TargetList.Remove(ToRemove);
            }
        }

        public async static Task QueueAdditionToList<T>(List<T> targetList, T toAdd, AsyncManualResetEvent runQueue)
        {
            await runQueue.WaitAsync();
            targetList.Add(toAdd);
        }

        public static List<T> CloneList<T>(IEnumerable<ICloneable> Original)
        {
            List<T> Result = new List<T>();
            foreach (ICloneable obj in Original)
            {
                Result.Add((T)obj.Clone());
            }
            return Result;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
