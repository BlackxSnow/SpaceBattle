using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Newtonsoft.Json;
using Entities.Parts.Weapons;
using System.IO;
using JsonConstructors;

namespace Management
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance;
        public static bool IsDataLoaded;
        public static event Action DataLoaded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple Datamanagers attempted to register ");
                Destroy(this);
                return;
            }
        }

        private async void Start()
        {
            await LoadAssets();
            LoadData();
            IsDataLoaded = true;
            DataLoaded?.Invoke();
        }

        const string WeaponPath = "Data/WeaponDefs";

        public static Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        public static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static Dictionary<string, WeaponConstructor> Weapons = new Dictionary<string, WeaponConstructor>();

        private async Task LoadAssets()
        {
            AsyncOperationHandle<IList<IResourceLocation>> prefabLocations = Addressables.LoadResourceLocationsAsync("Prefab");
            AsyncOperationHandle<IList<IResourceLocation>> textureLocations = Addressables.LoadResourceLocationsAsync("Texture");
            AsyncOperationHandle<IList<IResourceLocation>> materialLocations = Addressables.LoadResourceLocationsAsync("Material");
            await Task.WhenAll(prefabLocations.Task, textureLocations.Task);


            Task<Dictionary<string, GameObject>> populatePrefabs = PopulateDictionary<GameObject>(prefabLocations.Result);
            Task<Dictionary<string, Texture2D>> populateTextures = PopulateDictionary<Texture2D>(textureLocations.Result);
            Task<Dictionary<string, Material>> populateMaterials = PopulateDictionary<Material>(materialLocations.Result);
            await Task.WhenAll(populateMaterials, populatePrefabs, populateTextures);

            Prefabs = populatePrefabs.Result;
            Textures = populateTextures.Result;
            Materials = populateMaterials.Result;
        }

        private async Task<Dictionary<string, T>> PopulateDictionary<T>(IList<IResourceLocation> locations)
        {
            Dictionary<string, T> result = new Dictionary<string, T>();
            for (int i = 0; i < locations.Count; i++)
            {
                Task<T> asset = Addressables.LoadAssetAsync<T>(locations[i]).Task;
                await asset;
                result.Add(locations[i].PrimaryKey, asset.Result);
            }
            return result;
        }

        private void LoadData()
        {
            LoadWeapons();
        }

        private void LoadWeapons()
        {
            DataStruct<WeaponConstructor> weapons = LoadArrayedData<WeaponConstructor>(WeaponPath, out _);
            foreach(WeaponConstructor weapon in weapons.DataArray)
            {
                Weapons.Add(weapon.Name, weapon);
            }
        }

        [Serializable]
        public struct DataStruct<T>
        {
            public List<T> DataArray;
        }

        private DataStruct<T> LoadArrayedData<T>(string AssetPath, out int FileCount)
        {
            string FolderPath = Path.Combine(Application.streamingAssetsPath, AssetPath);
            DirectoryInfo Info = new DirectoryInfo(FolderPath);
            FileInfo[] Files = Info.GetFiles("*.json");

            DataStruct<T> AllLoadedData = new DataStruct<T>();
            AllLoadedData.DataArray = new List<T>();

            for (int i = 0; i < Files.Length; i++)
            {
                string FilePath = Path.Combine(FolderPath, Files[i].Name);
                string JsonData = File.ReadAllText(FilePath);
                DataStruct<T> LoadedData = JsonConvert.DeserializeObject<DataStruct<T>>(JsonData);

                AllLoadedData.DataArray = Utility.Collections.CombineLists(AllLoadedData.DataArray, LoadedData.DataArray);
            }
            FileCount = Files.Length;
            return AllLoadedData;
        }
    }
}
