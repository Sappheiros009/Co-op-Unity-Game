using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Shared prototype metadata for the seven chapter scenes and their story interludes.
    /// The palettes are intentionally broad placeholders, not final art direction.
    /// </summary>
    public sealed class PrototypeChapterDefinition
    {
        public int Number { get; }
        public string RegionName { get; }
        public string FolderName { get; }
        public string SceneName { get; }
        public string ScenePath { get; }
        public string StorySceneName { get; }
        public string StoryScenePath { get; }
        public string MapRootName { get; }
        public Color FloorColor { get; }
        public Color WallColor { get; }
        public Color AccentColor { get; }
        public Color ExitColor { get; }
        public Color HazardColor { get; }
        public Color AmbientColor { get; }
        public Color FogColor { get; }

        public string ChapterLabel => "Chapter " + Number.ToString("00") + " // " + RegionName;

        public PrototypeChapterDefinition(
            int number,
            string regionName,
            string folderName,
            Color floorColor,
            Color wallColor,
            Color accentColor,
            Color exitColor,
            Color hazardColor,
            Color ambientColor,
            Color fogColor)
        {
            Number = number;
            RegionName = regionName;
            FolderName = folderName;
            SceneName = "PrototypeChapter" + number.ToString("00");
            ScenePath = "Assets/Game/Levels/Episode01/" + folderName + "/" + SceneName + ".unity";
            StorySceneName = "PrototypeStoryInterlude_Chapter" + number.ToString("00");
            StoryScenePath = "Assets/Game/Levels/Episode01/StoryInterludes/" + StorySceneName + ".unity";
            MapRootName = folderName + "_Map";
            FloorColor = floorColor;
            WallColor = wallColor;
            AccentColor = accentColor;
            ExitColor = exitColor;
            HazardColor = hazardColor;
            AmbientColor = ambientColor;
            FogColor = fogColor;
        }
    }

    public static class PrototypeChapterCatalog
    {
        private static readonly PrototypeChapterDefinition[] Definitions =
        {
            new PrototypeChapterDefinition(
                1,
                "Mine",
                "Chapter01_Mine",
                new Color(0.18f, 0.21f, 0.28f),
                new Color(0.30f, 0.34f, 0.42f),
                new Color(0.38f, 0.22f, 0.50f),
                new Color(0.26f, 0.95f, 0.72f),
                new Color(0.95f, 0.22f, 0.08f),
                new Color(0.25f, 0.28f, 0.38f),
                new Color(0.08f, 0.10f, 0.16f)),
            new PrototypeChapterDefinition(
                2,
                "Lava Zone",
                "Chapter02_LAVA",
                new Color(0.28f, 0.10f, 0.06f),
                new Color(0.36f, 0.16f, 0.08f),
                new Color(0.82f, 0.28f, 0.05f),
                new Color(0.25f, 0.95f, 0.85f),
                new Color(1.00f, 0.08f, 0.02f),
                new Color(0.36f, 0.16f, 0.10f),
                new Color(0.16f, 0.04f, 0.02f)),
            new PrototypeChapterDefinition(
                3,
                "Polluted Zone",
                "Chapter03_PollutedZone",
                new Color(0.12f, 0.22f, 0.16f),
                new Color(0.20f, 0.30f, 0.18f),
                new Color(0.45f, 0.70f, 0.18f),
                new Color(0.35f, 1.00f, 0.70f),
                new Color(0.60f, 1.00f, 0.05f),
                new Color(0.18f, 0.28f, 0.20f),
                new Color(0.04f, 0.12f, 0.07f)),
            new PrototypeChapterDefinition(
                4,
                "Thunder Sea",
                "Chapter04_ThunderSea",
                new Color(0.05f, 0.12f, 0.22f),
                new Color(0.10f, 0.24f, 0.38f),
                new Color(0.16f, 0.55f, 0.86f),
                new Color(0.35f, 0.95f, 1.00f),
                new Color(0.98f, 0.72f, 0.08f),
                new Color(0.10f, 0.18f, 0.30f),
                new Color(0.02f, 0.05f, 0.14f)),
            new PrototypeChapterDefinition(
                5,
                "Square",
                "Chapter05_Square",
                new Color(0.28f, 0.25f, 0.22f),
                new Color(0.52f, 0.40f, 0.28f),
                new Color(0.83f, 0.55f, 0.20f),
                new Color(0.25f, 1.00f, 0.75f),
                new Color(0.94f, 0.20f, 0.12f),
                new Color(0.34f, 0.30f, 0.24f),
                new Color(0.12f, 0.10f, 0.08f)),
            new PrototypeChapterDefinition(
                6,
                "Frozen Mountain",
                "Chapter06_FrozenMountain",
                new Color(0.25f, 0.45f, 0.58f),
                new Color(0.58f, 0.78f, 0.88f),
                new Color(0.78f, 0.92f, 1.00f),
                new Color(0.45f, 1.00f, 0.85f),
                new Color(0.24f, 0.65f, 1.00f),
                new Color(0.48f, 0.64f, 0.74f),
                new Color(0.08f, 0.16f, 0.23f)),
            new PrototypeChapterDefinition(
                7,
                "Hometown",
                "Chapter07_Hometown",
                new Color(0.14f, 0.30f, 0.20f),
                new Color(0.22f, 0.42f, 0.28f),
                new Color(0.72f, 0.32f, 0.62f),
                new Color(0.35f, 1.00f, 0.78f),
                new Color(0.67f, 0.22f, 0.95f),
                new Color(0.22f, 0.38f, 0.28f),
                new Color(0.05f, 0.13f, 0.09f))
        };

        public static IReadOnlyList<PrototypeChapterDefinition> All => Definitions;

        public static PrototypeChapterDefinition Get(int chapterNumber)
        {
            foreach (var definition in Definitions)
            {
                if (definition.Number == chapterNumber)
                {
                    return definition;
                }
            }

            return Definitions[0];
        }
    }
}
