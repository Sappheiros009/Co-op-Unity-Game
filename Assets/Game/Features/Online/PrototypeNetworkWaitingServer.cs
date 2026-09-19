using System.Collections.Generic;
using UnityEngine;

namespace SlimeCoop.Prototype
{
    /// <summary>Server-only waiting-room physics, roster and station-distance authority.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PrototypeNetworkWaitingServer : MonoBehaviour
    {
        private sealed class Member
        {
            public PrototypeWaitingRoomWalker walker;
            public readonly PrototypeNetworkInputChannel input = new PrototypeNetworkInputChannel();
        }
        private readonly Dictionary<ulong,Member> _members = new Dictionary<ulong,Member>();
        private PrototypeNetworkTransport _network;
        private Transform _room;
        private PrototypeWaitingRoomStation[] _stations;
        private long _epoch, _tick;
        public void Configure(PrototypeNetworkTransport network)
        {
            _network=network;
            _room=PrototypeWaitingRoomLayout.Build(transform,true);
            _stations=_room.GetComponentsInChildren<PrototypeWaitingRoomStation>();
            _network.CaptureWaiting=Capture; _network.ServerStationGuard=ValidateStation;
            _network.WaitingInputReceived+=OnInput;
        }
        private void Update() { if (_network != null) Synchronize(_network.WaitingRoster(),_network.ServerWaitingEpoch,_network.ServerWaiting); }
        private void FixedUpdate()
        {
            // unscaledTime inside FixedUpdate is the fixed-step clock and can precede packet receipt.
            // Use the same monotonic real clock on both sides of the inbox.
            if (_network != null && _network.ServerWaiting) Step(Time.fixedDeltaTime,Time.realtimeSinceStartupAsDouble);
        }
        // Explicit inputs keep tests independent from a socket or the client's frame rate.
        public void Synchronize(PrototypeNetworkStartMember[] roster,long epoch,bool active)
        {
            if (_room==null) { _room=PrototypeWaitingRoomLayout.Build(transform,true); _stations=_room.GetComponentsInChildren<PrototypeWaitingRoomStation>(); }
            _room.gameObject.SetActive(active);
            if (!active) { foreach(var member in _members.Values) member.input.Clear(); return; }
            if (_epoch!=epoch)
            {
                foreach(var member in _members.Values) { member.walker.gameObject.SetActive(false); Destroy(member.walker.gameObject); }
                _members.Clear(); _epoch=epoch; _tick=0;
            }
            var departing=new List<ulong>();
            foreach(var pair in _members)
            {
                var keep=false; foreach(var entry in roster) if(entry.connection==pair.Key) { keep=true; break; }
                if(!keep) departing.Add(pair.Key);
            }
            foreach(var connection in departing)
            { var walker=_members[connection].walker; walker.gameObject.SetActive(false); Destroy(walker.gameObject); _members.Remove(connection); }
            foreach(var entry in roster) if(!_members.ContainsKey(entry.connection))
            {
                var obj=new GameObject("Server waiting slime "+entry.slot); obj.transform.SetParent(_room,false);
                var walker=obj.AddComponent<PrototypeWaitingRoomWalker>(); walker.Configure(entry.slot,entry.name,PrototypePlayerControl.Server,PrototypeWaitingRoomLayout.NetworkSpawn(entry.slot));
                foreach(var member in _members.Values) Physics.IgnoreCollision(walker.GetComponent<CharacterController>(),member.walker.GetComponent<CharacterController>());
                _members.Add(entry.connection,new Member{walker=walker});
            }
        }
        public bool Submit(ulong connection,PrototypeNetworkWaitingInput packet,double now,out string error)
        {
            error="unknown_connection";
            if(!_members.TryGetValue(connection,out var member)) return false;
            var stateError=!_room.gameObject.activeSelf || packet==null || packet.epoch!=_epoch ? "stale_waiting_room" : "";
            if(!member.input.Submit(packet?.sequence??0,packet?.input??PrototypePlayerInput.Neutral(),now,out error,stateError)) return false;
            // A command may follow the look intent in this same network tick. Position still changes only in Step.
            member.walker.SetLook(packet.input.yaw,packet.input.pitch); return true;
        }
        private void OnInput(ulong connection,PrototypeNetworkWaitingInput packet)
        {
            Synchronize(_network.WaitingRoster(),_network.ServerWaitingEpoch,_network.ServerWaiting);
            Submit(connection,packet,Time.realtimeSinceStartupAsDouble,out var error); _network.ReportWaitingVerdict(error);
        }
        public void Step(float seconds,double now)
        {
            if(_room==null || !_room.gameObject.activeSelf) return;
            foreach(var member in _members.Values) member.walker.StepServerInput(member.input.Consume(now),seconds);
            _tick++;
        }
        public PrototypeWaitingRoomWalker FindWalker(ulong connection) => _members.TryGetValue(connection,out var member) ? member.walker : null;
        public string ValidateStation(ulong connection,PrototypeNetworkCommand command)
        {
            if(_network!=null) Synchronize(_network.WaitingRoster(),_network.ServerWaitingEpoch,_network.ServerWaiting);
            if(_room==null || !_room.gameObject.activeSelf || !_members.TryGetValue(connection,out var member)) return "no_waiting_room";
            foreach(var station in _stations)
            {
                var target=command.kind=="chapter" ? station.Kind==PrototypeWaitingStationKind.Chapter && station.Chapter==command.chapter : station.Kind==PrototypeWaitingStationKind.Ready;
                if(target) return station.CanUse(member.walker,true) ? "" : "station_out_of_reach";
            }
            return "unknown_station";
        }
        public PrototypeNetworkWaitingState Capture()
        {
            if(_network!=null) Synchronize(_network.WaitingRoster(),_network.ServerWaitingEpoch,_network.ServerWaiting);
            var poses=new List<PrototypeNetworkWaitingPose>();
            foreach(var member in _members.Values)
            {
                var walker=member.walker;
                poses.Add(new PrototypeNetworkWaitingPose{slot=walker.Participant.ParticipantId,acknowledged=member.input.Sequence,position=walker.transform.position,
                    yaw=walker.transform.eulerAngles.y,pitch=walker.Pitch,crouching=walker.Participant.IsCrouching,cameraHeight=walker.ViewCamera.transform.localPosition.y});
            }
            poses.Sort((left,right)=>left.slot.CompareTo(right.slot));
            return new PrototypeNetworkWaitingState{epoch=_epoch,tick=_tick,actors=poses.ToArray()};
        }
        private void OnDestroy()
        {
            if(_network==null) return;
            _network.WaitingInputReceived-=OnInput; _network.CaptureWaiting=null; _network.ServerStationGuard=null;
        }
    }
}
