using HarmonyLib;
using InfiniteBeatSaber.Installers;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace InfiniteBeatSaber
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        private static Harmony _harmony;

        [Init]
        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// [Init] methods that use a Constructor or called before regular methods like InitWithConfig.
        /// Only use [Init] with one Constructor.
        /// </summary>
        public void Init(IPALogger logger, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;

            zenjector.Install<AppInstaller>(Location.App);
            zenjector.Install<MenuInstaller>(Location.Menu);
            zenjector.Install<PlayerInstaller>(Location.Player);

            Log.Info($"InfiniteBeatSaber initialized. Version: {BuildConstants.GitFullHash}. Build date: {BuildConstants.BuildDate.ToString("o")}");
        }

        [OnEnable]
        public void OnEnable()
        {
            try
            {
                _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        [OnDisable]
        public void OnDisable()
        {
            try
            {
                _harmony?.UnpatchSelf();
                _harmony = null;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
