using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using HarmonyLib;
using UnityEngine;

namespace MoreSelectionLocations
{
    [HarmonyPatch(typeof(FileSelectUIController), "RefreshDrivesList")]
    public class FileSelectUIController_RefreshDrivesList_AddExtraLocations
    {
        public static void Postfix(FileSelectUIController __instance)
        {
            Debug.Log("MoreSelectionLocations: Adding extra locations");
            //check if extraLocations.xml doesnt exists
            var extraLocations = new List<ExtraLocation>();
            var serializer = new XmlSerializer(typeof(List<ExtraLocation>));
            if (!File.Exists("extraLocations.xml"))
            {
                var example = new ExtraLocation()
                {
                    Name = "Example",
                    Path = "C:\\"
                };
                extraLocations.Add(example);
                using (var writer = new StreamWriter("extraLocations.xml"))
                {
                    serializer.Serialize(writer, extraLocations);
                }
                Debug.Log("Created extraLocations.xml");
                return;
            }
            
            //load extraLocations.xml
            using (var reader = new StreamReader("extraLocations.xml"))
            {
                extraLocations = (List<ExtraLocation>) serializer.Deserialize(reader);
            }
            
            //add extra location to __instance.DrivesScrollList
            foreach (var extraLocation in extraLocations)
            {
                __instance.DrivesScrollList.AddItem(extraLocation.Name, extraLocation.Path, 0, __instance.Sprites[3]);
            }
        }
    }
}