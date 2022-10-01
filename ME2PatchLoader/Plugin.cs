using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace ME2PatchLoader
{
    [BepInPlugin("dev.Sekoree.ME2PatchLoader", "ME2 Harmony Patch Loader", "0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        
        private void Awake()
        {
            harmony = new Harmony("dev.Sekoree.ME2PatchLoader");
            //Check if ME2Patches folder exists and if not create it
            if (!Directory.Exists(Path.Combine(Paths.GameRootPath, "ME2Patches")))
            {
                Directory.CreateDirectory(Path.Combine(Paths.GameRootPath, "ME2Patches"));
                Logger.LogInfo("ME2Patches folder created");
            }
            
            //Get all dll files in the ME2Patches folder
            var files = Directory.GetFiles(Path.Combine(Paths.GameRootPath, "ME2Patches"), "*.dll");
            foreach (var file in files)
            {
                //Load the assembly
                var assembly = Assembly.LoadFile(file);
                //Get all types in the assembly
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    //Check if the type has the HarmonyPatch attribute
                    if (type.GetCustomAttribute<HarmonyPatch>() != null)
                    {
                        //Patch the type
                        harmony.PatchAll(type);
                        Logger.LogInfo($"Patched {type.FullName}");
                    }
                }
            }
        }
    }
}
