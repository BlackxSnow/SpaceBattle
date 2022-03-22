using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utility
{
    public static class Architecture
    {
        /// <summary>
        /// Attempts to assign the 'instance' value to the reference 'instanceVar'. Throws and destroys instance if instanceVar is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instanceVar"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static void AssignSingleton<T>(ref T instanceVar, T instance) where T : MonoBehaviour
        {
            if (instanceVar != null)
            {
                UnityEngine.Object.Destroy(instance);
                throw new System.ArgumentException($"Instance was not null for {instance.GetType()}");
            }
            instanceVar = instance;
        }
    }
}
