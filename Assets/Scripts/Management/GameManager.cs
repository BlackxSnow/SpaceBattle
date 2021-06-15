using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Management
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public static Dictionary<string, StarSystem> Systems = new Dictionary<string, StarSystem>();
        public static Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
        public static event Action GameInitialized;
        public static bool IsGameInitialized;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new Exception($"Multiple GameManagers exist");
            }
            if (DataManager.IsDataLoaded)
            {
                OnDataLoaded();
            }
            else
            {
                DataManager.DataLoaded += OnDataLoaded;
            }
        }

        private void Start()
        {
            
        }

        public static bool RegisterFaction(string name)
        {
            if (Factions.ContainsKey(name))
            {
                return false;
            }
            else
            {
                Factions.Add(name, new Faction(name));
                return true;
            }
        }

        private void OnDataLoaded()
        {
            Systems.Add("Astraeus", new StarSystem());
            Systems["Astraeus"].SystemName = "Astraeus";
            IsGameInitialized = true;
            GameInitialized.Invoke();
        }
    }
}
