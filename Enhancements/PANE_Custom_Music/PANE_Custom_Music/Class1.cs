using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

// Author : Danie!, SpoonUnit
// Tested By : Danie!
// Tested With PANE Version : 1.5.3

namespace PANE_Custom_Music
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class PANECustomMusic : BaseUnityPlugin
    {
        private const string ModGUID = "PANE.Custom.Music";
        private const string ModName = "PANECustomMusic";
        private const string VersionString = "1.2.1";
        static ManualLogSource logger;

        // settings
        static ConfigEntry<bool> conf_enableMusic;
        private static readonly string customMusicFolder = Path.Combine(Path.GetDirectoryName(Paths.ExecutablePath), "ModResources/Music");
        private static readonly bool customMusicFolderExist = Directory.Exists(customMusicFolder);
        private static bool hasLoggedMissingFolder = false;

        // load patch and reference settings
        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModGUID} is loaded!");

            conf_enableMusic = Config.Bind("Music", "Enable Custom/OG Music", true, "Enable Custom Music (For correct apply needed save reload)");

            Harmony.CreateAndPatchAll(typeof(PANECustomMusic));
            logger.LogInfo($"{ModName} applied!");
        }

        [HarmonyPatch(typeof(AudioManager), "LoadMusicAudioClip")]
        [HarmonyPrefix]
        private static bool ApplyCustomMusicPatch(LevelMap.MapMusicData musicData)
        {
            try
            {
                if (!conf_enableMusic.Value)
                {
                    return true;
                }
                if (customMusicFolderExist)
                {
                    string matchingFile = FindMatchingFile(customMusicFolder, musicData.Music);

                    if (!string.IsNullOrEmpty(matchingFile))
                    {
                        logger.LogInfo($"Loading custom file: {matchingFile}");
                        musicData.Clip = LoadMusicClip(matchingFile);
                        musicData.Clip.name = matchingFile;

                        return false; // Skip the original method
                    }
                    else
                    {
                        logger.LogInfo($"Unable to load file: '{musicData.Music}'. File not exist.");
                        return true; // Continue with original method
                    }
                }
                else
                {
                    if (!hasLoggedMissingFolder)
                    {
                        logger.LogInfo($"\nUnable to load custom music folder.\nPlease create 'Music' folder inside this path: '{Path.GetDirectoryName(customMusicFolder)}' to achieve this path: '{customMusicFolder}'");
                        hasLoggedMissingFolder = true;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                logger.LogInfo($"Unable to load custom music: {e}");
                return true;
            }
        }

        [HarmonyPatch(typeof(AudioManager), "PlaySpecificMusic", new Type[] { typeof(AudioClip[]), typeof(bool) })]
        [HarmonyPrefix]
        private static bool PrefixPlaySpecificMusic(ref AudioClip[] clips)
        {

            try
            {
                if (!conf_enableMusic.Value)
                {
                    return true;
                }
                if (customMusicFolderExist && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        string clipName = clips[i].name;
                        string matchingFile = FindMatchingFile(customMusicFolder, clipName);
                        if (!string.IsNullOrEmpty(matchingFile))
                        {
                            logger.LogInfo($"Loading custom file: '{matchingFile}'.");
                            clips[i] = LoadMusicClip(matchingFile);
                            clips[i].name = matchingFile;

                        }
                        else
                        {
                            logger.LogInfo($"Unable to load file: '{clipName}'. File not exist.");
                        }
                    }
                    return true; // Continue with original method
                }
                else
                {
                    if (!hasLoggedMissingFolder)
                    {
                        logger.LogInfo($"\nUnable to load custom music folder.\nPlease create 'Music' folder inside this path: '{Path.GetDirectoryName(customMusicFolder)}' to achieve this path: '{customMusicFolder}'");
                        hasLoggedMissingFolder = true;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                logger.LogInfo($"Unable to play specific music: {e}");
                return true;
            }
        }

        [HarmonyPatch(typeof(AudioManager), "GetNextMusicClip")]
        [HarmonyPostfix]
        private static void PostfixGetNextMusicClipPatch(AudioClip __result)
        {
            logger.LogInfo($"Playing file: '{__result.name}'");
        }
        private static AudioClip LoadMusicClip(string path)
        {

            AudioClip musicClip = null;

            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, GetAudioType(path));
            www.SendWebRequest();

            while (!www.isDone)
            {
            }

            if (!www.isNetworkError && !www.isHttpError)
            {
                musicClip = DownloadHandlerAudioClip.GetContent(www);
            }
            else
            {
                logger.LogInfo($"Failed to load audio clip: {www.error}");
            }

            return musicClip;
        }


        private static string FindMatchingFile(string folderPath, string fileName)
        {
            string[] supportedExtensions = { ".wav", ".mp3" };

            foreach (string extension in supportedExtensions)
            {
                string fullPath = Path.Combine(folderPath, fileName + extension);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                string[] files = Directory.GetFiles(folderPath, "*" + extension);
                string matchingFile = files.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(matchingFile))
                {
                    return matchingFile;
                }
            }

            return null;
        }

        private static AudioType GetAudioType(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();

            switch (extension)
            {
                case ".wav":
                    return AudioType.WAV;
                case ".mp3":
                    return AudioType.MPEG;
                default:
                    return AudioType.UNKNOWN;
            }
        }
    }
}
