using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>
    /// Builds one fixed, readable chapter prototype map from primitives.
    /// All seven chapters share the same test geometry; their prototype palettes come from the catalog.
    /// </summary>
    public sealed class PrototypeMapBuilder : MonoBehaviour
    {
        private Vector3 _playerSpawn;
        private PrototypeCapsulePlayer _player;
        private PrototypeExitScoring _exit;

        public PrototypeChapterDefinition Definition { get; private set; }
        public PrototypeCapsulePlayer Player => _player;
        public PrototypeExitScoring Exit => _exit;
        public Vector3 PlayerSpawn => _playerSpawn;

        public void Build(PrototypeGame game)
        {
            var chapterNumber = game == null ? 1 : game.ChapterNumber;
            Definition = PrototypeChapterCatalog.Get(chapterNumber);

            var existingMapRoot = transform.Find(Definition.MapRootName);
            if (existingMapRoot != null)
            {
                RebindExistingScene(game, existingMapRoot);
                return;
            }

            var mapRoot = new GameObject(Definition.MapRootName).transform;
            mapRoot.SetParent(transform, false);
            var floorMaterial = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Floor Material", Definition.FloorColor);
            var wallMaterial = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Wall Material", Definition.WallColor);
            var accentMaterial = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Accent Material", Definition.AccentColor);
            var exitMaterial = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Exit Glow Material", Definition.ExitColor, true);
            var hazardMaterial = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Hazard Material", Definition.HazardColor, true);

            PrototypeVisuals.CreateCube(Definition.RegionName + " Floor", mapRoot, new Vector3(0f, -0.5f, 0f), new Vector3(38f, 1f, 30f), floorMaterial);
            PrototypeVisuals.CreateCube("North Wall", mapRoot, new Vector3(0f, 2f, 15f), new Vector3(38f, 4f, 1f), wallMaterial);
            PrototypeVisuals.CreateCube("South Wall", mapRoot, new Vector3(0f, 2f, -15f), new Vector3(38f, 4f, 1f), wallMaterial);
            PrototypeVisuals.CreateCube("East Wall", mapRoot, new Vector3(19f, 2f, 0f), new Vector3(1f, 4f, 30f), wallMaterial);
            PrototypeVisuals.CreateCube("West Wall", mapRoot, new Vector3(-19f, 2f, 0f), new Vector3(1f, 4f, 30f), wallMaterial);

            // Fixed rooms and corridors: a common readable route for all palette-only chapters.
            PrototypeVisuals.CreateCube("Central Divider A", mapRoot, new Vector3(-8f, 1.5f, 5f), new Vector3(8f, 3f, 1f), wallMaterial);
            PrototypeVisuals.CreateCube("Central Divider B", mapRoot, new Vector3(8f, 1.5f, 5f), new Vector3(8f, 3f, 1f), wallMaterial);
            PrototypeVisuals.CreateCube("East Pillar Wall", mapRoot, new Vector3(5f, 1.5f, -4f), new Vector3(1f, 3f, 10f), accentMaterial);
            PrototypeVisuals.CreateCube("West Pillar Wall", mapRoot, new Vector3(-7f, 1.5f, -5f), new Vector3(7f, 3f, 1f), accentMaterial);
            PrototypeVisuals.CreateCube("Upper Platform", mapRoot, new Vector3(11f, 0.75f, 2f), new Vector3(5f, 1.5f, 4f), accentMaterial);
            PrototypeVisuals.CreateCube("Lower Platform", mapRoot, new Vector3(-12f, 0.5f, 6f), new Vector3(4f, 1f, 4f), accentMaterial);

            var hazard = PrototypeVisuals.CreateCube(
                Definition.RegionName + " Warning Hazard Strip",
                mapRoot,
                new Vector3(0f, 0.03f, -1f),
                new Vector3(4f, 0.06f, 7f),
                hazardMaterial);
            var hazardCollider = hazard.GetComponent<BoxCollider>();
            hazardCollider.isTrigger = true;
            hazard.AddComponent<PrototypeHazard>();

            _playerSpawn = new Vector3(-14f, 0f, -10f);
            var exitPosition = new Vector3(14f, 0f, 10f);
            var exitMarker = PrototypeVisuals.CreateCube("Exit Floor Marker", mapRoot, exitPosition + new Vector3(0f, 0.04f, 0f), new Vector3(5f, 0.08f, 4f), exitMaterial);
            var exitGateLeft = PrototypeVisuals.CreateCube("Exit Gate Left", mapRoot, exitPosition + new Vector3(-2.2f, 2f, 0f), new Vector3(0.5f, 4f, 0.5f), exitMaterial);
            var exitGateRight = PrototypeVisuals.CreateCube("Exit Gate Right", mapRoot, exitPosition + new Vector3(2.2f, 2f, 0f), new Vector3(0.5f, 4f, 0.5f), exitMaterial);
            PrototypeVisuals.CreateCube("Exit Gate Top", mapRoot, exitPosition + new Vector3(0f, 4f, 0f), new Vector3(4.9f, 0.5f, 0.5f), exitMaterial);
            exitMarker.transform.SetSiblingIndex(0);
            exitGateLeft.transform.SetSiblingIndex(0);
            exitGateRight.transform.SetSiblingIndex(0);

            var exitTriggerObject = new GameObject("StartandExit_ExitTrigger");
            exitTriggerObject.transform.SetParent(mapRoot);
            exitTriggerObject.transform.position = exitPosition + Vector3.up;
            var exitCollider = exitTriggerObject.AddComponent<BoxCollider>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector3(4f, 2.2f, 3f);
            _exit = exitTriggerObject.AddComponent<PrototypeExitScoring>();
            _exit.Configure(exitTriggerObject.transform, 4, Time.time);

            SpawnPlayer(game, mapRoot);
            SpawnTeammates(game, mapRoot, exitPosition);
            SpawnMonsters(game, mapRoot);
            AddEnvironment(mapRoot);

            game.RegisterWorld(this, _player, _exit);
        }

        private void RebindExistingScene(PrototypeGame game, Transform mapRoot)
        {
            var playerObject = mapRoot.Find("Player_Capsule");
            if (playerObject != null)
            {
                _playerSpawn = playerObject.position;
                _player = playerObject.GetComponent<PrototypeCapsulePlayer>();
                if (_player == null)
                {
                    _player = playerObject.gameObject.AddComponent<PrototypeCapsulePlayer>();
                }

                _player.Configure(game, _playerSpawn, ReadBodyColor(playerObject, Definition.ExitColor));
            }

            var exitObject = mapRoot.Find("StartandExit_ExitTrigger");
            if (exitObject != null)
            {
                _exit = exitObject.GetComponent<PrototypeExitScoring>();
                if (_exit == null)
                {
                    _exit = exitObject.gameObject.AddComponent<PrototypeExitScoring>();
                }

                _exit.Configure(exitObject, 4, Time.time);
            }

            var exitPosition = exitObject != null ? exitObject.position : new Vector3(14f, 1f, 10f);
            var teammates = mapRoot.GetComponentsInChildren<PrototypeTeammate>(true);
            for (var index = 0; index < teammates.Length; index++)
            {
                var teammate = teammates[index];
                var spawnPoint = teammate.transform.position;
                var participantNumber = index + 2;
                var followOffset = index == 0
                    ? new Vector3(-2.5f, 0f, 1.5f)
                    : index == 1
                        ? new Vector3(2.5f, 0f, 1f)
                        : new Vector3(0f, 0f, -3f);
                teammate.Configure(
                    game,
                    index + 1,
                    "Slime " + participantNumber,
                    spawnPoint,
                    followOffset,
                    exitPosition,
                    ReadBodyColor(teammate.transform, Definition.AccentColor));
                game.RegisterTeammate(teammate);
            }

            var monsters = mapRoot.GetComponentsInChildren<PrototypeCapsuleMonster>(true);
            for (var index = 0; index < monsters.Length; index++)
            {
                var monster = monsters[index];
                var spawnPoint = monster.transform.position;
                monster.Configure(game, spawnPoint, ReadBodyColor(monster.transform, Definition.HazardColor));
                game.RegisterMonster(monster);
            }

            AddEnvironment(mapRoot);
            game.RegisterWorld(this, _player, _exit);
        }

        private void AddEnvironment(Transform mapRoot)
        {
            if (mapRoot.Find("Prototype Sun") == null)
            {
                PrototypeVisuals.CreateDirectionalLight(mapRoot);
            }

            RenderSettings.ambientLight = Definition.AmbientColor;
            RenderSettings.fog = true;
            RenderSettings.fogColor = Definition.FogColor;
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 95f;
        }

        private static Color ReadBodyColor(Transform root, Color fallback)
        {
            var renderer = root.GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial.color;
            }

            return fallback;
        }

        private void SpawnPlayer(PrototypeGame game, Transform mapRoot)
        {
            var playerObject = new GameObject("Player_Capsule");
            playerObject.transform.SetParent(mapRoot);
            _player = playerObject.AddComponent<PrototypeCapsulePlayer>();
            playerObject.AddComponent<PrototypeParticipant>();
            _player.Configure(game, _playerSpawn, Definition.ExitColor);
        }

        private void SpawnTeammates(PrototypeGame game, Transform mapRoot, Vector3 exitPosition)
        {
            var colors = new[]
            {
                new Color(1f, 0.56f, 0.72f),
                new Color(1f, 0.78f, 0.36f),
                new Color(0.68f, 1f, 0.46f)
            };
            var offsets = new[]
            {
                new Vector3(-2.5f, 0f, 1.5f),
                new Vector3(2.5f, 0f, 1f),
                new Vector3(0f, 0f, -3f)
            };
            var starts = new[]
            {
                new Vector3(-12f, 0f, -8f),
                new Vector3(-10f, 0f, -11f),
                new Vector3(-15f, 0f, -6f)
            };

            for (var index = 0; index < 3; index++)
            {
                var teammateObject = new GameObject("Teammate_" + (index + 2) + "_Capsule");
                teammateObject.transform.SetParent(mapRoot);
                var teammate = teammateObject.AddComponent<PrototypeTeammate>();
                teammateObject.AddComponent<PrototypeParticipant>();
                teammate.Configure(game, index + 1, "Slime " + (index + 2), starts[index], offsets[index], exitPosition, colors[index]);
                game.RegisterTeammate(teammate);
            }
        }

        private void SpawnMonsters(PrototypeGame game, Transform mapRoot)
        {
            var starts = new[]
            {
                new Vector3(1f, 0f, 9f),
                new Vector3(10f, 0f, -7f),
                new Vector3(-5f, 0f, 10f)
            };
            var monsterColor = Color.Lerp(Definition.HazardColor, Color.white, 0.15f);

            for (var index = 0; index < starts.Length; index++)
            {
                var monsterObject = new GameObject("Monster_Capsule_" + (index + 1));
                monsterObject.transform.SetParent(mapRoot);
                var monster = monsterObject.AddComponent<PrototypeCapsuleMonster>();
                monster.Configure(game, starts[index], monsterColor);
                game.RegisterMonster(monster);
            }
        }
    }

    public sealed class PrototypeHazard : MonoBehaviour
    {
        private float _damageCooldown;

        private void OnTriggerStay(Collider other)
        {
            var player = other.GetComponentInParent<PrototypeCapsulePlayer>();
            if (player == null || _damageCooldown > 0f)
            {
                return;
            }

            player.ReceiveDamage(8f);
            _damageCooldown = 0.75f;
        }

        private void Update()
        {
            _damageCooldown = Mathf.Max(0f, _damageCooldown - Time.deltaTime);
        }
    }
}
