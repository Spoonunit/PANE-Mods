using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private const string ModGUID = "PANE.Custom.Music";
        private const string ModName = "PANE_Custom_Music";
        private const string VersionString = "1.0.0";
        static ManualLogSource logger;
        private void Awake()
        {
            logger = Logger;
            logger.LogInfo($"Plugin {ModGUID} is loaded!");
            Harmony.CreateAndPatchAll(typeof(PANECustomMusic));
            logger.LogInfo($"{ModName} applied!");
        }

        [HarmonyPatch(typeof(AudioManager), "PlaySpecificMusic", new Type[] { typeof(AudioClip[]), typeof(bool) })]
        [HarmonyPrefix]
        private static bool PrefixPlaySpecificMusic(AudioManager __instance, ref float ____waitTimer, ref AudioSource ____MusicChannel, ref bool ____ready, ref bool ____shouldLoop, ref List<AudioClip> ____musicSpecificClips, ref float ____MusicVolume, AudioClip[] clips, bool shouldLoop = false)
        {
            try
            {
                logger.LogInfo($"{System.DateTime.Now} Looking for alternative for {clips[0].name}");
                string customMusicFolderPath = Path.Combine(Application.dataPath, "Music");
                logger.LogInfo($"{System.DateTime.Now} Checking in {customMusicFolderPath}");

                if (Directory.Exists(customMusicFolderPath) && clips.Length > 0)
                {
                    string clipName = clips[0].name;
                    string matchingFile = FindMatchingFile(customMusicFolderPath, clipName);

                    if (!string.IsNullOrEmpty(matchingFile))
                    {
                        AudioManager.Instance.StartCoroutine(LoadCustomMusicClip(matchingFile, __instance, clips, ____shouldLoop, shouldLoop, ____MusicChannel, ____waitTimer, ____ready, ____musicSpecificClips, ____MusicVolume));
                        return false; // Skip the original method
                    }
                }
                else
                {
                    logger.LogInfo($"\nUnable to load custom music folder.\nPlease create 'Music' folder inside this path: '{Application.dataPath}' to achieve this path: '{customMusicFolderPath}'");
                    return true; // Call the original method

                }
            }
            catch (Exception e)
            {
                logger.LogInfo($"Unable to play specific music: {e}");
            }
            return true; // Call the original method


        }

        [HarmonyPatch(typeof(AudioManager), "LoadMusicAudioClip")]
        [HarmonyPrefix]
        private static bool ApplyCustomMusicPatch(AudioManager __instance, LevelMap.MapMusicData musicData)
        {
            try
            {
                logger.LogInfo($"{System.DateTime.Now} Looking for alternative for {musicData.Music}");

                string musicPath = AudioManager.s_MusicAudioPath + "/" + musicData.Music;
                string customMusicFolderPath = Path.Combine(Application.dataPath, "Music");

                logger.LogInfo($"{System.DateTime.Now} Checking in {customMusicFolderPath}");

                if (Directory.Exists(customMusicFolderPath))
                {
                    string matchingFile = FindMatchingFile(customMusicFolderPath, musicData.Music);

                    if (!string.IsNullOrEmpty(matchingFile))
                    {
                        logger.LogInfo($"Loading custom file: {matchingFile}");
                        musicData.Clip = LoadMusicClip(matchingFile);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    logger.LogInfo("Unable to load custom music folder");
                }

                musicData.Clip = Resources.Load<AudioClip>(musicPath);
                return false;
            }
            catch (Exception e)
            {
                logger.LogInfo($"Unable to load custom music: {e}");
                return true;
            }
        }

        public static IEnumerator LoadCustomMusicClip(string path, AudioManager __instance, AudioClip[] clips, bool ____shouldLoop, bool shouldLoop, AudioSource ____MusicChannel, float ____waitTimer, bool ____ready, List<AudioClip> ____musicSpecificClips, float ____MusicVolume)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, GetAudioType(path)))
            {
                yield return www.SendWebRequest();
                
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(www);
                logger.LogInfo($"{System.DateTime.Now} Loaded Custom Audio Clip from {path} ... Length:{myClip.length} Channels:{myClip.channels}");
                ____ready = true;
                ____shouldLoop = shouldLoop; 
                ____MusicChannel.clip = myClip;
                ____MusicChannel.volume = 1f;
                ____MusicChannel.Play();
            }
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
