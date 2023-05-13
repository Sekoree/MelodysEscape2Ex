using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ME2Diag
{
    [HarmonyPatch(typeof(Track), "PopulateNodesIntensityTable")]
    public class SpeedMultiNew
    {
        public static void Prefix(Track __instance, ref float[] speedMultipliers)
        {
            Debug.Log("Speed Multiplier: " + speedMultipliers.Length);
            speedMultipliers[0] = 1.2f;
            speedMultipliers[1] = 1.5f;
            speedMultipliers[2] = 1.8f;
            speedMultipliers[3] = 2.1f;
            
            
            //get internal float[] CurrentSpeedMult from __instance.Generator
            var field = typeof(TrackGenerator).GetField("CurrentSpeedMult", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(__instance.Generator, speedMultipliers);
            
        }
    }

    //[HarmonyPatch(typeof(TrackDefinition), "Initialize")]
    //public class DensityLog
    //{
    //    public static void Postfix(TrackDefinition __instance)
    //    {
    //        //get private int "a" from TrackDefinition with reflection
    //        //var a = typeof(TrackDefinition).GetField("a", BindingFlags.NonPublic | BindingFlags.Instance);
    //        //a?.SetValue(__instance, 18);
    //        //Debug.Log($"min hold = {a?.GetValue(__instance)}");
    //        __instance.RegularSpacing = 12;
    //        Debug.Log($"RegularSpacing = {__instance.RegularSpacing}");
    //        __instance.CloseSpacing = 9;
    //        Debug.Log($"CloseSpacing = {__instance.CloseSpacing}");
    //    }
    //}

    //[HarmonyPatch(typeof(Obstacle), "CreateObjectAndGeometry")]
    //public class ObstacleScaleTest
    //{
    //    public static bool Prefix(Obstacle __instance, ref TrackGenerator trackGenerator, ref int startPadding)
    //    {
    //        TrackNode trackNode = MGame.Track.Nodes[__instance.StartSampleID + startPadding];
    //        TrackNode trackNode2 = MGame.Track.Nodes[__instance.RenderingStartSampleID + startPadding];
    //        Quaternion rotation = (__instance.IsMeshProcedurallyGenerated
    //            ? Quaternion.identity
    //            : (trackNode2.Rotation * __instance.ObstacleSettings.RotationOffset));
    //        Vector3 positionOffset = __instance.ObstacleSettings.PositionOffset;
    //        if (!__instance.IsMeshProcedurallyGenerated)
    //        {
    //            positionOffset += trackNode2.Position;
    //        }
//
    //        GameObject prefab = __instance.ObstacleSettings.Prefab;
    //        GameObject prefabB = __instance.ObstacleSettings.PrefabB;
    //        if (prefab == null)
    //        {
    //            Debug.LogError("Prefab not found for Obstacle " + __instance.Type);
    //            return false;
    //        }
//
    //        bool flag = __instance.Intensity != IntensityLevel.Low;
    //        __instance.SizeMult = 1f;
    //        if (flag)
    //        {
    //            float num = trackGenerator.CurrentSpeedMult[(int)__instance.Intensity];
    //            __instance.SizeMult = ((num * 1f) - 1f) * 0.5f + 1.1f;
    //        }
//
    //        GameObject gameObject = UnityEngine.Object.Instantiate(prefab, positionOffset, rotation);
    //        //Debug.Log($"Obstacle {__instance.Type} created with size {__instance.SizeMult}");
    //        if (gameObject != null)
    //        {
    //            gameObject.transform.parent = MGame.TrackGenerator.ObstaclesParentObject.transform;
    //            ObstacleController component = gameObject.GetComponent<ObstacleController>();
    //            component.Obstacle = __instance;
    //            __instance.GameObject = gameObject;
    //            __instance.Controller = component;
    //            if (flag && __instance.IsLightZone)
    //            {
    //                Vector3 localScale = gameObject.transform.localScale;
    //                if ((double)__instance.SizeMult > 1.001)
    //                {
    //                    localScale.x *= __instance.SizeMult;
    //                    localScale.z *= __instance.SizeMult;
    //                    gameObject.transform.localScale = localScale;
    //                }
    //            }
    //            //Debug.Log($"Obstacle setting stuff if not null");
    //        }
    //        else
    //        {
    //            Debug.LogError(string.Format("Could not instantiate obstacle prefab of type {0}!", __instance.Type));
    //        }
//
    //        if (prefabB != null)
    //        {
    //            Debug.Log($"prefabB is not null");
    //            GameObject gameObject2 = UnityEngine.Object.Instantiate(prefabB, positionOffset, rotation);
    //            if (gameObject != null)
    //            {
    //                gameObject2.transform.parent = MGame.TrackGenerator.ObstaclesParentObject.transform;
    //                ObstacleController component2 = gameObject2.GetComponent<ObstacleController>();
    //                component2.Obstacle = __instance;
    //                __instance.GameObjectB = gameObject2;
    //                __instance.ControllerB = component2;
    //                if (__instance.ControllerB.IsLightOrbTrail)
    //                {
    //                    gameObject2.transform.rotation = trackNode2.Rotation;
    //                }
    //            }
    //            else
    //            {
    //                Debug.LogError(string.Format("Could not instantiate obstacle prefab B of type {0}!",
    //                    __instance.Type));
    //            }
    //        }
//
    //        __instance.ColorID = -1;
    //        
    //        //get internal field IsPSXGamepad from InputController with reflection
    //        //Debug.Log("Getting IsPSXGamepad");
    //        var isPSXGamepad = typeof(InputController).GetField("IsPSXGamepad", BindingFlags.NonPublic | BindingFlags.Instance);
    //        var isPSXGamepadValue = (bool)isPSXGamepad?.GetValue(MGame.InputController);
    //        //Debug.Log($"IsPSXGamepad = {isPSXGamepadValue}");
    //        
    //        if (isPSXGamepadValue || SettingsManager.Settings.GamepadIconsType == 2)
    //        {
    //            __instance.ColorID = Obstacle.GetPSXColorID(__instance.InputType);
    //        }
    //        else
    //        {
    //            __instance.ColorID = Obstacle.GetColorID(__instance.InputType);
    //        }
//
    //        Color color;
    //        if (__instance.ColorID >= 0)
    //        {
    //            color = MGame.TrackGenerator.ObstacleColors[__instance.ColorID];
    //        }
    //        else
    //        {
    //            Debug.LogError(string.Format("Invalid ColorID: {0} for InputType {1}", __instance.ColorID,
    //                __instance.InputType));
    //            color = Color.magenta;
    //        }
//
    //        __instance.InputColorA = color;
    //        __instance.InputColorB = color;
    //        if (MGame.CurrentDifficultyRules.InputMethod == InputMethod.DirectionOrColor && !__instance.IsLightOrb &&
    //            !__instance.IsLightZone)
    //        {
    //            __instance.InputColorA = MGame.TrackGenerator.ObstacleColors[4];
    //        }
//
    //        __instance.CurrentColorA = __instance.InputColorA;
    //        __instance.CurrentColorB = __instance.InputColorA;
    //        if (flag)
    //        {
    //            //Debug.Log($"flag is true");
    //            Quaternion quaternion = Quaternion.Euler(0f, Obstacle.GetIndicatorRotation(__instance.InputType), 0f);
    //            float num2 = Obstacle.GetIndicatorScale(__instance.Intensity) * __instance.SizeMult;
    //            GameObject gameObject3 = (__instance.IsLightZone
    //                ? trackGenerator.ObstacleLightZoneIndicatorPrefab
    //                : trackGenerator.ObstacleSolidIndicatorPrefab);
    //            //Debug.Log($"gameObject3 = {gameObject3}");
    //            GameObject gameObject4 = UnityEngine.Object.Instantiate(gameObject3,
    //                trackNode.Position + gameObject3.transform.position,
    //                trackNode.Rotation * gameObject3.transform.rotation * quaternion);
    //            //Debug.Log($"gameObject4 = {gameObject4}");
    //            gameObject4.SetActive(false);
    //            gameObject4.transform.parent = MGame.TrackGenerator.ObstaclesParentObject.transform;
    //            ObstacleIndicatorController component3 = gameObject4.GetComponent<ObstacleIndicatorController>();
    //            __instance.IndicatorGameObject = gameObject4;
    //            __instance.IndicatorController = component3;
    //            GameObject gameObject5 = (__instance.IsLightZone
    //                ? trackGenerator.ObstacleLightZoneFillIndicatorPrefab
    //                : trackGenerator.ObstacleSolidFillIndicatorPrefab);
    //            //Debug.Log($"gameObject5 = {gameObject5}");
    //            GameObject gameObject6 = UnityEngine.Object.Instantiate(gameObject5,
    //                trackNode.Position + gameObject5.transform.position,
    //                trackNode.Rotation * gameObject5.transform.rotation);
    //            //Debug.Log($"gameObject6 = {gameObject6}");
    //            gameObject6.transform.parent = gameObject4.transform;
    //            ObstacleIndicatorController component4 = gameObject6.GetComponent<ObstacleIndicatorController>();
    //            float indicatorInputSpriteOffset = Obstacle.GetIndicatorInputSpriteOffset(__instance.InputType);
    //            component4.SetSpriteOffset(indicatorInputSpriteOffset);
    //            ObstacleIndicatorController b = null;
    //            if (__instance.IsHeld)
    //            {
    //                GameObject obstacleHeldIndicatorPrefab = trackGenerator.ObstacleHeldIndicatorPrefab;
    //                //Debug.Log($"obstacleHeldIndicatorPrefab = {obstacleHeldIndicatorPrefab}");
    //                GameObject gameObject7 = UnityEngine.Object.Instantiate(obstacleHeldIndicatorPrefab,
    //                    trackNode.Position + obstacleHeldIndicatorPrefab.transform.position,
    //                    trackNode.Rotation * obstacleHeldIndicatorPrefab.transform.rotation);
    //                //Debug.Log($"gameObject7 = {gameObject7}");
    //                gameObject7.transform.parent = gameObject4.transform;
    //                b = gameObject7.GetComponent<ObstacleIndicatorController>();
    //            }
//
    //            component3.SetSubIndicators(component4, b);
    //            Vector3 localScale2 = gameObject4.transform.localScale;
    //            localScale2.x *= num2;
    //            localScale2.z *= num2;
    //            gameObject4.transform.localScale = localScale2;
    //        }
//
    //        if (__instance.IsMeshProcedurallyGenerated)
    //        {
    //            //Debug.Log($"IsMeshProcedurallyGenerated is true");
    //            //get private field ShaderObstacleSize from Obstacle with reflection
    //            var shaderObstacleSize = typeof(Obstacle).GetField("ShaderObstacleSize", BindingFlags.NonPublic | BindingFlags.Instance);
    //            shaderObstacleSize?.SetValue(__instance, __instance.RenderingEndSampleID - __instance.RenderingStartSampleID);
    //            
    //            float centerZCoord = 0f;
    //            List<ExtrusionPoint> list = new List<ExtrusionPoint>();
    //            for (int i = __instance.RenderingStartSampleID; i <= __instance.RenderingEndSampleID; i++)
    //            {
    //                TrackNode trackNode3 = MGame.Track.Nodes[i + startPadding];
    //                Vector3 position = trackNode3.Position;
    //                Quaternion rotation2 = trackNode3.Rotation;
    //                if (__instance.ObstacleSettings.CurvedEnd)
    //                {
    //                    int num3 = 8;
    //                    int num4 = __instance.RenderingEndSampleID - num3;
    //                    if (i >= num4)
    //                    {
    //                        float num5 = (float)(i - num4) / (float)num3;
    //                        num5 = 1f - Mathf.Cos(num5 * (float)Math.PI * 0.5f);
    //                        float num6 = Mathf.Lerp(0f, -0.25f, num5);
    //                        position.y += num6;
    //                        rotation2 *= Quaternion.Euler(Mathf.Lerp(0f, 30f, num5), 0f, 0f);
    //                    }
    //                }
//
    //                list.Add(new ExtrusionPoint(position, rotation2, trackNode3.Color, trackNode3.LocalMagnitude,
    //                    trackNode3.TotalMagnitude, false, false));
    //            }
//
    //            //Debug.Log($"list.Count = {list.Count}");
    //            MeshCrossLoopExtruder.ExtrudeAlongsidePoints(gameObject, false, Vector3.zero, list, 0, list.Count - 1,
    //                centerZCoord, VertexColorMode.ObstacleProgress, false);
    //            //Debug.Log("MeshCrossLoopExtruder.ExtrudeAlongsidePoints");
    //        }
//
    //        gameObject.SetActive(false);
    //        return false;
    //    }
    //}
}