using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeCoop.Prototype.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string LobbyScenePath = "Assets/Game/Levels/Lobby/PrototypeLobby.unity";
        private const string WaitingRoomScenePath = "Assets/Game/Levels/Lobby/PrototypeWaitingRoom.unity";

        public static void BuildScenes()
        {
            CreateDirectoryForAsset(LobbyScenePath);
            CreateDirectoryForAsset(WaitingRoomScenePath);
            CreateDirectoryForAsset("Assets/Game/Levels/Episode01/StoryInterludes/PrototypeStoryInterlude_Chapter01.unity");

            BuildLobbyScene();
            BuildWaitingRoomScene();
            foreach (var chapter in PrototypeChapterCatalog.All)
            {
                CreateDirectoryForAsset(chapter.ScenePath);
                CreateDirectoryForAsset(chapter.StoryScenePath);
                BuildChapterScene(chapter);
                BuildStoryInterludeScene(chapter);
            }

            var sceneSettings = new List<EditorBuildSettingsScene>();
            foreach (var scenePath in GetPrototypeScenePaths())
            {
                sceneSettings.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = sceneSettings.ToArray();
            PlayerSettings.productName = "Slime Co-op Prototype";
            PlayerSettings.companyName = "Sappheiros009";
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrototypeSceneBuilder] Built Lobby -> WaitingRoom -> Chapter01..07 with story interludes.");
            EditorApplication.Exit(0);
        }

        public static string[] GetPrototypeScenePaths()
        {
            var paths = new List<string>
            {
                LobbyScenePath,
                WaitingRoomScenePath
            };

            foreach (var chapter in PrototypeChapterCatalog.All)
            {
                paths.Add(chapter.ScenePath);
                paths.Add(chapter.StoryScenePath);
            }

            return paths.ToArray();
        }

        private static void BuildLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Lobby_Root");
            var controller = root.AddComponent<SlimeCoop.Prototype.PrototypeLobbyController>();
            PrototypeVisuals.UseWhiteSpriteForEditorPreview(EnsureWhiteSpriteAsset());
            controller.BuildRoom();
            PersistSceneMaterials(scene, "Lobby");
            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        private static void BuildWaitingRoomScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("WaitingRoom_Root");
            var controller = root.AddComponent<SlimeCoop.Prototype.PrototypeWaitingRoomController>();
            controller.BuildRoom();
            PersistSceneMaterials(scene, "WaitingRoom");
            EditorSceneManager.SaveScene(scene, WaitingRoomScenePath);
        }

        private static void BuildChapterScene(PrototypeChapterDefinition chapter)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Chapter" + chapter.Number.ToString("00") + "_Root");
            var game = root.AddComponent<SlimeCoop.Prototype.PrototypeGame>();
            game.ConfigureChapter(chapter.Number);
            var mapBuilder = root.AddComponent<SlimeCoop.Prototype.PrototypeMapBuilder>();
            mapBuilder.Build(game);
            PersistSceneMaterials(scene, "Chapter" + chapter.Number.ToString("00"));
            EditorSceneManager.SaveScene(scene, chapter.ScenePath);
        }

        private static void BuildStoryInterludeScene(PrototypeChapterDefinition chapter)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("StoryInterlude_Chapter" + chapter.Number.ToString("00") + "_Root");
            var controller = root.AddComponent<SlimeCoop.Prototype.PrototypeStoryInterludeController>();
            controller.ConfigureChapter(chapter.Number);
            PrototypeVisuals.UseWhiteSpriteForEditorPreview(EnsureWhiteSpriteAsset());
            controller.BuildRoom();
            PersistSceneMaterials(scene, "StoryInterlude_Chapter" + chapter.Number.ToString("00"));
            EditorSceneManager.SaveScene(scene, chapter.StoryScenePath);
        }

        private static Sprite EnsureWhiteSpriteAsset()
        {
            const string assetPath = "Assets/Game/Core/Prototype/PrototypeWhiteSprite.asset";
            var existingSprite = FindSpriteAsset(assetPath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            CreateDirectoryForAsset(assetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, false)
                {
                    name = "Prototype White Texture"
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply(false, true);
                AssetDatabase.CreateAsset(texture, assetPath);
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            sprite.name = "Prototype White Sprite";
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static Sprite FindSpriteAsset(string assetPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                var sprite = asset as Sprite;
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static void PersistSceneMaterials(Scene scene, string scenePrefix)
        {
            const string materialFolder = "Assets/Game/Core/Prototype/Materials";
            CreateDirectoryForAsset(materialFolder + "/placeholder.mat");

            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                foreach (var renderer in sceneRoot.GetComponentsInChildren<Renderer>(true))
                {
                    var material = renderer.sharedMaterial;
                    if (material == null || AssetDatabase.Contains(material))
                    {
                        continue;
                    }

                    var materialName = SanitizeFileName(material.name);
                    var assetPath = materialFolder + "/" + scenePrefix + "_" + materialName + ".mat";
                    var persistentMaterial = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (persistentMaterial == null)
                    {
                        persistentMaterial = Object.Instantiate(material);
                        persistentMaterial.name = scenePrefix + "_" + materialName;
                        AssetDatabase.CreateAsset(persistentMaterial, assetPath);
                    }

                    renderer.sharedMaterial = persistentMaterial;
                    EditorUtility.SetDirty(renderer);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidCharacter, '_');
            }

            return value.Replace(' ', '_');
        }

        private static void CreateDirectoryForAsset(string assetPath)
        {
            var fullPath = Path.GetFullPath(assetPath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
