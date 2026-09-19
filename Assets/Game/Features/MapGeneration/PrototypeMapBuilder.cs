using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Seeded three-room test course, five normal stages and a cooperative boss variant.</summary>
    public sealed class PrototypeMapBuilder : MonoBehaviour
    {
        private PrototypeGame _game;
        private Transform _root;
        private Material _floor, _wall, _accent, _danger, _safe;
        private readonly System.Collections.Generic.List<PrototypeInteractable> _pickups = new System.Collections.Generic.List<PrototypeInteractable>();
        private PrototypeInteractable _keySource;
        private float _recoveryAt;
        public System.Collections.Generic.IReadOnlyList<PrototypeInteractable> Pickups => _pickups;
        public PrototypeChapterDefinition Definition { get; private set; }
        public PrototypeCapsulePlayer Player { get; private set; }
        public PrototypeExitScoring Exit { get; private set; }
        public Vector3 PlayerSpawn => new Vector3(0, 0.15f, -5);
        public PrototypeRoomLayout Layout { get; private set; }
        public PrototypeMovingRaft Raft { get; private set; }

        public void Build(PrototypeGame game)
        {
            _game = game; Definition = game.Chapter;
            Layout = new PrototypeRoomLayout(PrototypeSession.Seed + game.ChapterNumber * 101 + game.StageNumber * 13);
            if (!Layout.Validate()) throw new System.InvalidOperationException("진행 불가능한 연결 통로");
            var old = transform.Find(Definition.MapRootName);
            if (old != null)
            {
                old.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
            }
            _root = new GameObject(Definition.MapRootName).transform;
            _root.SetParent(transform, false);
            _floor = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Floor", Definition.FloorColor);
            _wall = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Wall", Definition.WallColor);
            _accent = PrototypeVisuals.CreateMaterial(Definition.RegionName + " Accent", Definition.AccentColor);
            _danger = PrototypeVisuals.CreateMaterial("Warning", new Color(0.95f, 0.18f, 0.12f), true);
            _safe = PrototypeVisuals.CreateMaterial("Safe", new Color(0.2f, 0.9f, 0.7f), true);

            for (var room = 0; room < 3; room++)
            {
                var z = room * 24f;
                Cube("Room_" + room + "_Floor", new Vector3(0, -0.5f, z), new Vector3(20, 1, 16), _floor);
                Cube("WestWall_" + room, new Vector3(-10, 2, z), new Vector3(0.5f, 4, 16), _wall);
                Cube("EastWall_" + room, new Vector3(10, 2, z), new Vector3(0.5f, 4, 16), _wall);
                DoorWall(room + "_South", z - 8, room == 0 ? 0 : Layout.CorridorX[room - 1], room == 0);
                DoorWall(room + "_North", z + 8, room == 2 ? 0 : Layout.CorridorX[room], room == 2);
            }
            for (var i = 0; i < 2; i++)
            {
                var x = Layout.CorridorX[i]; var z = 12 + i * 24;
                Cube("SeededCorridor_" + i, new Vector3(x, -0.5f, z), new Vector3(4, 1, 9), _floor);
                Cube("CorridorLeft_" + i, new Vector3(x - 2, 0.65f, z), new Vector3(0.3f, 1.3f, 9), _wall);
                Cube("CorridorRight_" + i, new Vector3(x + 2, 0.65f, z), new Vector3(0.3f, 1.3f, 9), _wall);
            }
            var climb = Cube("MarkedClimbWall", new Vector3(-7, 1.75f, 2), new Vector3(4, 3.5f, 0.8f), _accent);
            climb.AddComponent<PrototypeClimbSurface>();
            Cube("ClimbLedge", new Vector3(-7, 3.3f, 3), new Vector3(4, 0.4f, 2.5f), _accent);
            Cube("CrouchPassage", new Vector3(6, 1.35f, 1), new Vector3(3, 0.4f, 3), _wall);
            Label(game.HasNetworkPlayers ? "이동 연습 | 표시된 벽: 등반 / 낮은 통로: 앉기" : "이동 연습  |  표시된 벽: 좌클릭 / 낮은 통로: Ctrl", new Vector3(0, 3.5f, 5), 0.17f);

            var exitObject = new GameObject("StartandExit_ExitTrigger");
            exitObject.transform.SetParent(_root); exitObject.transform.position = new Vector3(0, 0.1f, 53);
            Exit = exitObject.AddComponent<PrototypeExitScoring>();
            Exit.Configure(exitObject.transform, Application.isPlaying ? PrototypeSession.StartingCount : 4, Time.time);
            Exit.SetGateOpen(false);
            Cube("ExitFloorMarker", new Vector3(0, 0.025f, 53), new Vector3(4, 0.05f, 3), _safe, false);
            Cube("ExitLeft", new Vector3(-2.2f, 1.8f, 53), new Vector3(0.3f, 3.6f, 0.4f), _safe);
            Cube("ExitRight", new Vector3(2.2f, 1.8f, 53), new Vector3(0.3f, 3.6f, 0.4f), _safe);
            Cube("ExitTop", new Vector3(0, 3.6f, 53), new Vector3(4.7f, 0.3f, 0.4f), _safe);
            Label("출구 / EXIT", new Vector3(0, 4.3f, 53), 0.25f);

            var objective = _root.gameObject.AddComponent<PrototypeStageObjective>(); objective.Configure(game);
            for (var i = 0; i < objective.RequiredRoles; i++)
            {
                var pad = Cube("CoopPad_" + i, new Vector3(-4.5f + i * 3f, 0.05f, 47), new Vector3(2.2f, 0.1f, 2.2f), _safe);
                objective.AddPad(pad.transform);
                Label("장치 " + (i + 1), pad.transform.position + Vector3.up * 1.2f, 0.14f);
            }
            if (game.ChapterNumber != 4)
            {
                Device("지역 장치 A", new Vector3(-6, 0.8f, 23), PrototypeInteractionKind.Region, objective, 0);
                Device("지역 장치 B", new Vector3(6, 0.8f, 25), game.ChapterNumber == 5 ? PrototypeInteractionKind.Record : PrototypeInteractionKind.Region, objective, 1);
            }
            Device("열쇠 연결", new Vector3(-7, 0.8f, 42), PrototypeInteractionKind.KeySocket, objective, 0);
            _pickups.Clear();
            _keySource = Item(PrototypeItemKind.Key, Layout.ItemPositions[0]);
            Item(PrototypeItemKind.Medkit, Layout.ItemPositions[1]);
            Item(PrototypeItemKind.Lure, Layout.ItemPositions[2]);
            Item(PrototypeItemKind.Rope, new Vector3(-7, 4f, 3));
            var memory = PrototypeVisuals.CreateSphere("Necklace_MemoryEcho", _root, new Vector3(-7,4.3f,3), Vector3.one * .3f, _safe);
            memory.AddComponent<PrototypeStoryMemory>().Configure(game);
            Label(game.HasNetworkPlayers ? "목걸이 기억" : "목걸이 기억 · N", new Vector3(-7,5,3), .13f);

            var box = Cube("Carryable_PuzzleBox", new Vector3(4, 0.7f, 41), Vector3.one * 1.2f, _accent);
            var carry = box.AddComponent<PrototypeCarryable>(); carry.Configure(false);
            objective.RequiredBox = carry; objective.BoxSocket = new Vector3(7, 0.7f, 45);
            Cube("BoxSocket", new Vector3(7, 0.06f, 45), new Vector3(2.4f, 0.12f, 2.4f), _safe, false);
            Label("상자 소켓", new Vector3(7, 1.5f, 45), 0.15f);
            var large = Cube("Pushable_HeavyBox", new Vector3(6, 0.75f, -4), Vector3.one * 1.5f, _accent);
            large.AddComponent<PrototypeCarryable>().Configure(true);

            // Accessible to an isolated survivor; never an automatic role-count failure.
            Hazard("FatalRoute", new Vector3(8.6f, 0.12f, 50), new Vector3(1.8f, 0.25f, 5), 120f);
            Label("위험! 사망 경로", new Vector3(8, 2.4f, 51), 0.13f);
            BuildRegion(objective);
            var playerObject = new GameObject("Player_Capsule"); playerObject.transform.SetParent(_root);
            Player = playerObject.AddComponent<PrototypeCapsulePlayer>();
            var control = game.IsReplica ? PrototypePlayerControl.Replica : game.IsServerControlled ? PrototypePlayerControl.Server : PrototypePlayerControl.Local;
            Player.Configure(game, PlayerSpawn, Definition.ExitColor, 0, game.HasNetworkPlayers ? game.ServerPlayerName(0) : "나", control);
            game.RegisterWorld(this, Player, Exit);
            var count = Application.isPlaying ? PrototypeSession.StartingCount : 4;
            var colors = new[] { new Color(1, 0.55f, 0.7f), new Color(1, 0.8f, 0.3f), new Color(0.6f, 0.9f, 1) };
            for (var i = 1; i < count; i++)
            {
                var teammateObject = new GameObject("Teammate_" + (i + 1)); teammateObject.transform.SetParent(_root);
                if (game.HasNetworkPlayers)
                {
                    teammateObject.name = "Player_" + (i + 1);
                    var controlled = teammateObject.AddComponent<PrototypeCapsulePlayer>();
                    controlled.Configure(game, new Vector3(-4 + i * 2, .15f, -3), colors[i - 1], i,
                        game.ServerPlayerName(i), control);
                    game.RegisterControlledPlayer(controlled);
                    continue;
                }
                var teammate = teammateObject.AddComponent<PrototypeTeammate>();
                teammate.Configure(game, i, "동료 " + (i + 1), new Vector3(-4 + i * 2, 0.15f, -3),
                    new Vector3((i - 2) * 1.8f, 0, -2), Exit.EntryPoint.position, colors[i - 1]);
                game.RegisterTeammate(teammate);
            }
            if (game.StageNumber > 1)
            {
                var monsterObject = new GameObject("Monster_Sentry"); monsterObject.transform.SetParent(_root);
                var monster = monsterObject.AddComponent<PrototypeCapsuleMonster>();
                monster.Configure(game, new Vector3(0, 0.15f, 28), Definition.HazardColor);
                game.RegisterMonster(monster);
            }
            if (game.StageNumber == 6)
            {
                var boss = PrototypeVisuals.CreateSphere("Boss_EnvironmentGuardian", _root, new Vector3(0, 3.2f, 55), Vector3.one * 3, _danger);
                Label("공동 장치로 3회 작동 · 붉은 예고 시 가장자리 대피", new Vector3(0, 5.4f, 51), 0.17f);
                var warning = Cube("BossAttackArea", new Vector3(0,.025f,49), new Vector3(14,.035f,10), _danger, false);
                objective.AttackArea = warning.GetComponent<Renderer>();
            }
            PrototypeVisuals.CreateDirectionalLight(_root);
            RenderSettings.ambientLight = Definition.AmbientColor;
            RenderSettings.fog = true; RenderSettings.fogColor = Definition.FogColor;
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 45; RenderSettings.fogEndDistance = 110;
        }
        private void BuildRegion(PrototypeStageObjective objective)
        {
            switch (_game.ChapterNumber)
            {
                case 2:
                    Hazard("Lava", new Vector3(0, 0.12f, 24), new Vector3(7, 0.25f, 8), 24);
                    for (var i = 0; i < 3; i++)
                    {
                        var platform = Cube("CoolingPlatform_" + i, new Vector3(0, 0.45f, 21 + i * 3), new Vector3(2.5f, 0.6f, 2.3f), _accent);
                        platform.AddComponent<PrototypeCyclePlatform>().Offset = i * 1.2f;
                    }
                    break;
                case 3:
                    var pollution = Hazard("Pollution", new Vector3(0, 0.12f, 24), new Vector3(6, 0.25f, 7), 12);
                    pollution.DisableWhenReady = objective;
                    break;
                case 4:
                    Hazard("DeepWater", new Vector3(0, 0.12f, 24), new Vector3(7, 0.25f, 8), 8);
                    var raft = Cube("FloatingRaft", new Vector3(0, .125f, 21), new Vector3(3.5f, .25f, 3), _accent);
                    Raft = raft.AddComponent<PrototypeMovingRaft>(); Raft.Configure(_game, objective);
                    var waterValve = Device("물속 잠금 해제", new Vector3(3, .5f, 24), PrototypeInteractionKind.WaterValve, objective, 0);
                    waterValve.Raft = Raft;
                    var wheel = Device("부유물 조타 · 전원 탑승", new Vector3(0,.8f,21.7f), PrototypeInteractionKind.RaftWheel, objective, 1);
                    wheel.Raft = Raft; wheel.transform.SetParent(raft.transform, true);
                    Label(_game.HasNetworkPlayers ? "물속 잠금 해제 → 전원 직접 탑승 → 조타 장치 사용" : "물속에서 잠금 해제 → T 동료 탑승 → E 조타", new Vector3(0,3,24), .18f);
                    break;
                case 6:
                    var ice = Cube("SlipperyIce", new Vector3(0, 0.035f, 24), new Vector3(10, 0.07f, 10), _safe);
                    ice.AddComponent<PrototypeIceSurface>();
                    break;
                default:
                    Cube("RegionMonument", new Vector3(0, 1.5f, 25), new Vector3(2, 3, 2), _accent);
                    break;
            }
            Label(objective.RegionalLabel, new Vector3(0, 4.7f, 29), 0.22f);
        }
        private void DoorWall(string id, float z, float x, bool closed)
        {
            if (closed) { Cube(id, new Vector3(0, 2, z), new Vector3(20, 4, 0.5f), _wall); return; }
            var leftWidth = x + 8; var rightWidth = 8 - x;
            if (leftWidth > 0) Cube(id + "_L", new Vector3(-10 + leftWidth / 2, 2, z), new Vector3(leftWidth, 4, 0.5f), _wall);
            if (rightWidth > 0) Cube(id + "_R", new Vector3(10 - rightWidth / 2, 2, z), new Vector3(rightWidth, 4, 0.5f), _wall);
        }
        private GameObject Cube(string name, Vector3 p, Vector3 size, Material m, bool collision = true) =>
            PrototypeVisuals.CreateCube(name, _root, p, size, m, collision);
        private void Label(string text, Vector3 p, float size) { PrototypeUi.WorldLabel(_root, text, p, size); }
        private PrototypeInteractable Device(string label, Vector3 position, PrototypeInteractionKind kind, PrototypeStageObjective objective, int id)
        {
            var obj = Cube(label, position, new Vector3(1, 1.6f, 1), _accent);
            var device = obj.AddComponent<PrototypeInteractable>();
            device.Kind = kind; device.Objective = objective; device.DeviceId = id; device.Label = label;
            objective.RegisterDevice(device);
            Label(label + (_game.HasNetworkPlayers ? "" : " [E]"), position + Vector3.up * 1.4f, 0.14f);
            return device;
        }
        private PrototypeInteractable Item(PrototypeItemKind item, Vector3 p)
        {
            var obj = Cube("Item_" + item, p, Vector3.one * 0.55f, _safe);
            var pickup = obj.AddComponent<PrototypeInteractable>();
            pickup.Kind = PrototypeInteractionKind.Pickup; pickup.Item = item; pickup.Label = PrototypeInventory.Label(item);
            PrototypeUi.WorldLabel(pickup.transform, pickup.Label, p + Vector3.up, .22f);
            _pickups.Add(pickup); return pickup;
        }
        public void DropItem(PrototypeItemKind kind, Vector3 position)
        {
            PrototypeInteractable pickup = null;
            foreach (var candidate in _pickups)
                if (candidate.Item == kind && !candidate.gameObject.activeSelf) { pickup = candidate; break; }
            if (pickup == null) pickup = Item(kind, position);
            else { pickup.RestorePickup(); pickup.transform.position = position; }
            if (kind == PrototypeItemKind.Key && _keySource != null) pickup.SetRecoveryOrigin(_keySource.RecoveryOrigin);
            var body = pickup.GetComponent<Rigidbody>();
            if (body == null) body = pickup.gameObject.AddComponent<Rigidbody>(); // Unity native fake-null must not use ?? here.
            body.isKinematic = false; body.linearVelocity = Vector3.zero; body.mass = .4f; body.constraints = RigidbodyConstraints.FreezeRotation;
        }
        public PrototypeInteractable AddReplicaPickup(PrototypeItemKind kind, Vector3 position)
        {
            if (_game == null || !_game.IsReplica || _pickups.Count >= 32) throw new System.InvalidOperationException("Only a bounded display world can add replica pickups.");
            var pickup = Item(kind, position); pickup.enabled = false;
            foreach (var collider in pickup.GetComponentsInChildren<Collider>()) collider.enabled = false;
            return pickup;
        }
        private void Update()
        {
            if (_game == null || _keySource == null || Time.time < _recoveryAt) return;
            _recoveryAt = Time.time + .5f;
            foreach (var actor in _game.Participants)
            {
                if (actor.IsConnected && actor.Inventory.Has(PrototypeItemKind.Key)) return;
            }
            foreach (var pickup in _pickups)
                if (pickup != null && pickup.Item == PrototypeItemKind.Key && pickup.gameObject.activeInHierarchy) return;
            foreach (var actor in _game.Participants)
                while (!actor.IsConnected && actor.Inventory.Has(PrototypeItemKind.Key)) actor.Inventory.Consume(PrototypeItemKind.Key);
            _keySource.RestorePickup();
            _game.SetStatus("회수할 수 없는 필수 열쇠를 첫 방의 지정 위치에 복구했습니다.");
        }
        private PrototypeHazard Hazard(string name, Vector3 position, Vector3 size, float dps)
        {
            var obj = Cube(name, position, size, _danger);
            obj.GetComponent<BoxCollider>().isTrigger = true;
            var hazard = obj.AddComponent<PrototypeHazard>(); hazard.DamagePerSecond = dps;
            return hazard;
        }
    }
}
