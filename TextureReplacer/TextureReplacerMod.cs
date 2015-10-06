﻿using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using PrefabHook;

namespace TextureReplacer
{
    public class TextureReplacerMod : LoadingExtensionBase, IUserMod
    {
        public string Name
        {
            get { return "Texture Replacer"; }
        }
        public string Description
        {
            get { return "Replaces the textures of the game assets with remastered versions"; }
        }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            // cancel if Prefab Hook is not installed
            if (!IsHooked()) return;

            TextureReplacer.instance.OnCreated();

            BuildingInfoHook.OnPreInitialization += TextureReplacer.instance.ProcessBuilding;
            BuildingInfoHook.Deploy();
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            // display warning when level is loaded if Prefab Hook is not installed
            if (!IsHooked())
            {
                UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel").SetMessage(
                    "Missing dependency", 
                    "Texture Replacer requires the 'Prefab Hook' mod to work properly. Please subscribe to the mod and restart the game!", 
                    false);
                return;
            }

            TextureReplacer.instance.OnLevelLoaded();
        }

        public override void OnReleased()
        {
            base.OnReleased();

            if (!IsHooked()) return;

            BuildingInfoHook.Revert();
        }

        // checks if the player subscribed to the Prefab Hook mod
        private bool IsHooked()
        {
            foreach (PluginManager.PluginInfo current in PluginManager.instance.GetPluginsInfo())
            {
                if (current.publishedFileID.AsUInt64 == 530771650uL) return true;
            }
            return false;
        }
    }
}
