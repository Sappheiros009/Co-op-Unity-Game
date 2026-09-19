using System;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Read-only client world. Seeded geometry is built once per stage; only server poses drive actors and props.</summary>
    public sealed class PrototypeNetworkReplica : MonoBehaviour
    {
        private Transform[] _props = Array.Empty<Transform>();
        private MaterialPropertyBlock _warning;
        private Vector3[] _positions;
        private Quaternion[] _rotations;
        private float _yaw, _pitch;
        public PrototypeGame Game { get; private set; }
        public PrototypeNetworkWorldState State { get; private set; }
        public Camera ViewCamera { get; private set; }
        public int BuildCount { get; private set; }
        public bool LocalLook { get; set; } = true;

        public bool Apply(PrototypeNetworkWorldState state)
        {
            if (state == null || !state.IsValid() || state.yourActor < 0
                || (State != null && state.sequence <= State.sequence)) return false;
            var rebuild = Game == null || State == null || state.runId != State.runId || state.stage != State.stage;
            // A fixed run cannot silently change participants, seed or chapter midway through a stage.
            if (State != null && state.runId == State.runId && (state.chapter != State.chapter || state.seed != State.seed
                || state.stage < State.stage || state.actors.Length != State.actors.Length
                || state.yourActor != State.yourActor || !SameRoster(State,state))) return false;
            if (rebuild) Build(state);
            State = state;
            if (_props.Length != state.props.Length) throw new InvalidOperationException("Seeded server/client prop layout differs.");
            for (var i=0;i<state.actors.Length;i++)
            {
                var player = Game.ControlledPlayers[i]; var actor = state.actors[i];
                player.Participant.ApplyReplicaStatus(actor);
                player.gameObject.SetActive(actor.connected);
                _positions[i] = actor.position; _rotations[i] = Quaternion.Euler(0,actor.yaw,0);
                if (rebuild || Vector3.Distance(player.transform.position,actor.position)>3)
                    player.transform.SetPositionAndRotation(actor.position,_rotations[i]);
                player.ViewCamera.transform.localPosition = Vector3.up * actor.cameraHeight;
            }
            if (rebuild || !LocalLook) { _yaw = state.actors[state.yourActor].yaw; _pitch = state.actors[state.yourActor].pitch; }
            for (var i=0;i<_props.Length;i++) ApplyPose(_props[i],state.props[i]);
            while (Game.Map.Pickups.Count < state.pickups.Length)
            {
                var pickup = state.pickups[Game.Map.Pickups.Count];
                Game.Map.AddReplicaPickup(pickup.kind,pickup.pose.position);
            }
            for (var i=0;i<Game.Map.Pickups.Count;i++)
            {
                var pickup = Game.Map.Pickups[i];
                if (i>=state.pickups.Length) { pickup.gameObject.SetActive(false); continue; }
                if (pickup.Item != state.pickups[i].kind) throw new InvalidOperationException("Server/client pickup identity differs.");
                ApplyPose(pickup.transform,state.pickups[i].pose);
            }
            var attackArea = Game.Objective.AttackArea;
            if (attackArea != null)
            {
                attackArea.enabled = state.bossWarning || state.bossDanger;
                _warning ??= new MaterialPropertyBlock();
                var color = state.bossDanger ? new Color(1,.08f,.02f) : new Color(1,.65f,.05f);
                _warning.SetColor("_BaseColor",color); _warning.SetColor("_EmissionColor",color); attackArea.SetPropertyBlock(_warning);
            }
            Game.gameObject.SetActive(state.phase=="Playing");
            return true;
        }
        private static bool SameRoster(PrototypeNetworkWorldState before,PrototypeNetworkWorldState after)
        {
            for (var i=0;i<before.actors.Length;i++)
                if (before.actors[i].slot!=after.actors[i].slot || before.actors[i].name!=after.actors[i].name) return false;
            return true;
        }
        private void Build(PrototypeNetworkWorldState state)
        {
            Clear();
            PrototypeSession.PrepareReplicaStage(state.chapter,state.stage,state.runId,state.actors.Length,state.seed);
            var root = new GameObject("Network Replica Chapter "+state.chapter+" Stage "+state.stage); root.transform.SetParent(transform,false);
            Game = root.AddComponent<PrototypeGame>(); Game.ConfigureChapter(state.chapter);
            var names = new string[state.actors.Length]; for(var i=0;i<names.Length;i++) names[i]=state.actors[i].name;
            Game.ConfigureReplicaPlayers(names);
            var map = root.AddComponent<PrototypeMapBuilder>(); map.Build(Game);
            _props = PrototypeNetworkWorldState.DynamicProps(Game);
            // Disable only gameplay behaviours. TMP and body-part animation remain presentation components.
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour.GetType().Namespace==typeof(PrototypeGame).Namespace && !(behaviour is PrototypeSlimeBody)) behaviour.enabled=false;
            foreach (var body in root.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic=true; body.detectCollisions=false; }
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            _positions = new Vector3[names.Length]; _rotations = new Quaternion[names.Length];
            var self = Game.ControlledPlayers[state.yourActor];
            self.transform.Find("CharacterParts/Head").gameObject.SetActive(false);
            ViewCamera = self.ViewCamera; ViewCamera.enabled=true; ViewCamera.tag="MainCamera";
            ViewCamera.gameObject.AddComponent<AudioListener>();
            BuildCount++;
        }
        private static void ApplyPose(Transform target,PrototypeNetworkPose pose)
        { target.SetPositionAndRotation(pose.position,pose.rotation); target.gameObject.SetActive(pose.active); }
        public void SetLook(float yaw,float pitch)
        { if(float.IsFinite(yaw)&&float.IsFinite(pitch)) { _yaw=Mathf.Repeat(yaw,360); _pitch=Mathf.Clamp(pitch,-82,82); } }
        public Vector2 Look => new Vector2(_yaw,_pitch);
        private void LateUpdate()
        {
            if (Game==null || State==null || State.phase!="Playing") return;
            var blend=1-Mathf.Exp(-25*Time.unscaledDeltaTime);
            for(var i=0;i<_positions.Length;i++)
            {
                var target=Game.ControlledPlayers[i].transform;
                target.SetPositionAndRotation(Vector3.Lerp(target.position,_positions[i],blend),Quaternion.Slerp(target.rotation,_rotations[i],blend));
            }
            ViewCamera.fieldOfView=PrototypeSettings.Current.fieldOfView;
            ViewCamera.transform.rotation=Quaternion.Euler(_pitch,_yaw,0);
        }
        public void Clear()
        {
            if(Game!=null) { Game.gameObject.SetActive(false); Destroy(Game.gameObject); Game=null; }
            ViewCamera=null; _props=Array.Empty<Transform>();
        }
        private void OnDestroy() { Clear(); }
    }
}
