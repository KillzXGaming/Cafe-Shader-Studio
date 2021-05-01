using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using TrackStudioLibrary.Turbo;
using GLFrameworkEngine;
using Toolbox.Core.IO;

namespace BfresEditor
{
    public class MapLoader
    {
        public static bool HasValidPath => File.Exists($"{GlobalSettingsMK8.MarioKart8Path}\\Data\\objflow.byaml");

        public static List<GenericRenderer> Renders = new List<GenericRenderer>();

        //Only loads skybox for loading into rendered cubemaps
        public static void LoadSkybox(string fileName)
        {
            LoadObjectDB($"{GlobalSettingsMK8.MarioKart8Path}\\Data\\objflow.byaml");

            CourseDefinition course = new CourseDefinition(fileName);
            foreach (var obj in course.Objs)
                LoadMapObject(obj, true);
        }

        public static void LoadMuunt(string fileName) {
            LoadObjectDB($"{GlobalSettingsMK8.MarioKart8Path}\\Data\\objflow.byaml");

            CourseDefinition course = new CourseDefinition(fileName);
            foreach (var obj in course.Objs)
                LoadMapObject(obj);
        }

        static void LoadObjectDB(string path) {
            if (GlobalSettingsMK8.IDLookup.Count > 0)
                return;

            var objTable = new ObjDefinitionDb(path);
            foreach (var obj in objTable.Definitions)
                GlobalSettingsMK8.IDLookup.Add(obj.ObjId, obj);
        }

        static void LoadMapObject(Obj obj, bool onlyLoadSkybox = false) {
            var resource = GetResourceName(obj);
            if (resource.Contains("Moon") || resource.Contains("Sun"))
                return;

            if (!resource.Contains("VR") && onlyLoadSkybox)
                return;

            string path = FindFilePath(resource);
            if (path == string.Empty)
                return;

            var render = BfresRender.LoadFile(path);

            //Use fustrum culling on non sky box objects
            if (!resource.Contains("VR"))
                render.StayInFustrum = false;
            else
                render.IsSkybox = true;

            if (render.IsSkybox)
            {
                //Temporary atm but disable the background as it has render conflicts with the skybox
                CafeStudio.UI.DrawableBackground.Display = false;
            }

            LoadBFRES(obj, render);
        }

        static void LoadBFRES(Obj obj, GenericRenderer render) {
            render.Transform.Position = new OpenTK.Vector3(
                obj.Translate.X,
                obj.Translate.Y,
                obj.Translate.Z);
            render.Transform.RotationEulerDegrees = new OpenTK.Vector3(
             obj.RotateDegrees.X,
             obj.RotateDegrees.Y,
             obj.RotateDegrees.Z);
            render.Transform.Scale = new OpenTK.Vector3(
                obj.Scale.X,
                obj.Scale.Y,
                obj.Scale.Z);
            render.Transform.UpdateMatrix(true);

            Renders.Add(render);
        }

        static string GetResourceName(Obj obj)
        {
            if (GlobalSettingsMK8.IDLookup.ContainsKey(obj.ObjId))
                return GlobalSettingsMK8.IDLookup[obj.ObjId].ResNames.FirstOrDefault();
            else if (GlobalSettingsMK8.ObjectList.ContainsKey(obj.ObjId))
                return GlobalSettingsMK8.ObjectList[obj.ObjId];
            else
                return "";
        }

        static string FindFilePath(string resName)
        {
            //Common path for common race objects like coins
            string raceObjects = $"{GlobalSettingsMK8.MarioKart8Path}\\race_common\\{resName}\\{resName}.bfres";
            string raceObjectsDX = $"{GlobalSettingsMK8.MarioKart8Path}\\RaceCommon\\{resName}\\{resName}.bfres";

            //The typical path for the base game map objects
            string mapObjects = $"{GlobalSettingsMK8.MarioKart8Path}\\mapobj\\{resName}\\{resName}.bfres";
            string mapObjectsDX = $"{GlobalSettingsMK8.MarioKart8Path}\\MapObj\\{resName}\\{resName}.bfres";

            if (File.Exists(raceObjects)) return raceObjects;
            if (File.Exists(raceObjectsDX)) return raceObjectsDX;
            if (File.Exists(mapObjectsDX)) return mapObjectsDX;
            if (File.Exists(mapObjects)) return mapObjects;

            if (Directory.Exists(GlobalSettingsMK8.MarioKart8AOCPath))
                return FindAOCPath(GlobalSettingsMK8.MarioKart8AOCPath, resName);

            return string.Empty;
        }

        static string FindAOCPath(string aocContent, string resName)
        {
            if (!aocContent.EndsWith("content"))
                aocContent = $"{aocContent}\\content";

            //Loop each DLC course (00##) folder
            foreach (var dir in Directory.GetDirectories(aocContent))
            {
                string mapObject = $"{dir}\\mapobj\\{resName}\\{resName}.bfres";
                if (File.Exists(mapObject))
                    return mapObject;
            }

            return string.Empty;
        }
    }
}
