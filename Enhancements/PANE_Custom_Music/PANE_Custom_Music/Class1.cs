using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Application = UnityEngine.Application;

namespace PANE_Custom_Music
{
    [BepInPlugin(ModGUID, ModName, VersionString)]
    public class PANECustomMusic : BaseUnityPlugin
    {
        private const string ModGUID = "PANE.ENH.Custom.Music";
        private const string ModName = "Custom Music (Enhancement)";
        private const string VersionString = "1.0.0";
        static ManualLogSource logger;

        // settings
        private static ConfigEntry<bool> enabled;
        
        // load patch and reference settings
        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModGUID} is loaded!");

            // to do - determine if changes to setting value are immediately impactful or does the game need to be reloaded
            // list of settings - these will be surfaced in configuration managed if the mod dll is present in bepinex/plugins

            enabled = Config.Bind(  "Settings",
                                    "On/Off",
                                    false,
                                    "Enable Custom Music");

            Harmony.CreateAndPatchAll(typeof(PANECustomMusic));
            logger.LogInfo($"{ModName} applied!");
        }

        // specific mod content

        [HarmonyPatch(typeof(AudioManager), "LoadMusicAudioClip")]
        [HarmonyPrefix]
        private static bool ApplyCustomMusicPatch(LevelMap.MapMusicData musicData)
        {
            try
            {
                string musicPath = AudioManager.s_MusicAudioPath + "/" + musicData.Music;
                string customMusicFolderPath = Path.Combine(Application.dataPath, "Music");
                if (Directory.Exists(customMusicFolderPath))
                {
                    string matchingFile = FindMatchingFile(customMusicFolderPath, musicData.Music);

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
                    logger.LogInfo($"\nUnable to load custom music folder.\nPlease create 'Music' folder inside this path: '{Application.dataPath}' to achieve this path: '{customMusicFolderPath}'");
                }

                musicData.Clip = Resources.Load<AudioClip>(musicPath);

                return false; // Skip the original method
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
                string customMusicFolderPath = Path.Combine(Application.dataPath, "Music");

                if (Directory.Exists(customMusicFolderPath) && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        string clipName = clips[i].name;
                        string matchingFile = FindMatchingFile(customMusicFolderPath, clipName);
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
                    logger.LogInfo($"\nUnable to load custom music folder.\nPlease create 'Music' folder inside this path: '{Application.dataPath}' to achieve this path: '{customMusicFolderPath}'");
                    return true; // Continue with original method
                }
            }
            catch (Exception e)
            {
                logger.LogInfo($"Unable to play specific music: {e}");
            }
            return true; // Continue with original method
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
