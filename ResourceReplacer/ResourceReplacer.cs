﻿using System;
using System.Collections.Generic;
using ColossalFramework.IO;
using ResourceReplacer.Pack;
using ResourceReplacer.Packs;
using UnityEngine;

namespace ResourceReplacer {
    // TODO restore original textures on reload
    // TODO live reload
    public class ResourceReplacer : ModSingleton<ResourceReplacer> {
        public readonly List<ResourcePack> ActivePacks = new List<ResourcePack>();

        private readonly Dictionary<string, Texture> _originalBuildingTextures = new Dictionary<string, Texture>();
        private readonly Dictionary<TextureKey, Texture2D> _replacementBuildingTextures = new Dictionary<TextureKey, Texture2D>();

        private readonly Dictionary<string, ResourcePack.PrefabColors> _originalBuildingColors = new Dictionary<string, ResourcePack.PrefabColors>();


        #region Textures
        public void ReplaceAllBuildingTextures() {
            var prefabCount = PrefabCollection<BuildingInfo>.LoadedCount();
            for (var i = 0u; i < prefabCount; i++) ReplaceBuildingTextures(PrefabCollection<BuildingInfo>.GetLoaded(i));
        }

        public void ReplaceBuildingTextures(BuildingInfo prefab) {
            if (prefab == null) return;

            ReplaceBuildingTextures(prefab.GetComponent<Renderer>(), prefab.name, false);

            if (prefab.m_lodObject != null) {
                ReplaceBuildingTextures(prefab.m_lodObject.GetComponent<Renderer>(), prefab.name, true);
            }
        }

        private void ReplaceBuildingTextures(Renderer renderer, string prefabName, bool lod) {
            if (renderer == null) return;

            var material = renderer.sharedMaterial; 
            if (material == null) return;

            foreach (var propertyName in TextureNames.Properties.Keys) {
                var texture = material.GetTexture(propertyName);

                var textureName = TextureNames.GetReplacementTextureName(prefabName, texture, propertyName, lod);

                var replacementTexture = GetReplacementBuildingTexture(textureName);
                if (replacementTexture != null) {
                    // Save original texture for restoration purposes
                    if (TextureNames.IsOriginalTexture(texture)) _originalBuildingTextures[textureName] = texture;

                    #if DEBUG
                    UnityEngine.Debug.Log($"Replacing texture {textureName}");
                    #endif

                    // Apply replacement texture
                    material.SetTexture(propertyName, replacementTexture);
                }
            }
        }

        private Texture2D GetReplacementBuildingTexture(string textureName) {
            foreach (var pack in ActivePacks) {
                var key = new TextureKey(pack.Path, textureName);

                if (_replacementBuildingTextures.TryGetValue(key, out var replacementTexture)) {
                    return replacementTexture;
                }

                replacementTexture = pack.GetBuildingTexture(textureName);
                if (replacementTexture != null) {
                    // Set correct name and add to cache
                    replacementTexture.name = TextureNames.ReplacedTexturePrefix + textureName;
                    _replacementBuildingTextures[key] = replacementTexture;

                    return replacementTexture;
                }
            }

            return null;
        }

        public void RestoreAllBuildingTextures() {
            var prefabCount = PrefabCollection<BuildingInfo>.LoadedCount();
            for (var i = 0u; i < prefabCount; i++) RestoreBuildingTextures(PrefabCollection<BuildingInfo>.GetLoaded(i));
        }

        public void RestoreBuildingTextures(BuildingInfo prefab) {
            if (prefab == null) return;
            
            RestoreBuildingTextures(prefab.GetComponent<Renderer>(), prefab.name, false);

            if (prefab.m_lodObject != null) {
                RestoreBuildingTextures(prefab.m_lodObject.GetComponent<Renderer>(), prefab.name, false);
            }
        }

        private void RestoreBuildingTextures(Renderer renderer, string prefabName, bool lod) {
            if (renderer == null) return;

            var material = renderer.sharedMaterial;
            if (material == null) return;

            foreach (var propertyName in TextureNames.Properties.Keys) {
                var texture = material.GetTexture(propertyName);
                if (TextureNames.IsOriginalTexture(texture)) continue;

                var textureName = TextureNames.GetReplacementTextureName(prefabName, texture, propertyName, lod);

                if (_originalBuildingTextures.TryGetValue(textureName, out var originalTexture)) {
                    #if DEBUG
                    UnityEngine.Debug.Log($"Restoring original texture for {textureName}");
                    #endif
                    material.SetTexture(propertyName, originalTexture);
                }
            }
        }
        #endregion

        #region Color Variations
        public void SetBuildingColors(BuildingInfo prefab, ResourcePack.PrefabColors colors) {
            if (prefab == null) return;

            if (!_originalBuildingColors.ContainsKey(prefab.name)) {
                _originalBuildingColors[prefab.name] = new ResourcePack.PrefabColors {
                    UseColorVariation = prefab.m_useColorVariations,
                    Color0 = prefab.m_color0,
                    Color1 = prefab.m_color1,
                    Color2 = prefab.m_color2,
                    Color3 = prefab.m_color3
                };
            }
            ApplyColors(prefab, colors);
        }

        public void RestoreAllBuildingColors() {
            foreach (var prefabName in _originalBuildingColors.Keys) {
                RestoreBuildingColors(PrefabCollection<BuildingInfo>.FindLoaded(prefabName));
            }
        }

        public void RestoreBuildingColors(BuildingInfo prefab) {
            if (prefab == null) return;
            if (_originalBuildingColors.TryGetValue(prefab.name, out var colors)) {
                ApplyColors(prefab, colors);
            }
        }

        private static void ApplyColors(BuildingInfo prefab, ResourcePack.PrefabColors colors) {
            prefab.m_useColorVariations = colors.UseColorVariation;
            prefab.m_color0 = colors.Color0;
            prefab.m_color1 = colors.Color1;
            prefab.m_color2 = colors.Color2;
            prefab.m_color3 = colors.Color3;
        }
        #endregion

        public void ClearCache() {                    
            #if DEBUG
            UnityEngine.Debug.Log($"Clearing texture cache...");
            #endif

            _originalBuildingTextures.Clear();

            foreach (var texture in _replacementBuildingTextures.Values) {
                UnityEngine.Object.DestroyImmediate(texture, true);
            }

            _replacementBuildingTextures.Clear();
        }

        private readonly struct TextureKey : IEquatable<TextureKey>
        {
            private readonly string Path;
            private readonly string Name;

            public TextureKey(string path, string name) {
                Path = path;
                Name = name;
            }

            public bool Equals(TextureKey other)
            {
                return Path == other.Path && Name == other.Name;
            }

            public override bool Equals(object obj)
            {
                return obj is TextureKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (Path.GetHashCode() * 397) ^ Name.GetHashCode();
            }
        }
    }
}
