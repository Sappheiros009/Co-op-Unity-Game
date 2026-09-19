using UnityEngine;

namespace SlimeCoop.Prototype
{
    public sealed class PrototypeHazard : MonoBehaviour
    {
        public float DamagePerSecond = 15;
        public PrototypeStageObjective DisableWhenReady;
        private PrototypeGame _game;
        private Collider _volume;
        private void Awake()
        {
            _game=GetComponentInParent<PrototypeGame>(); _volume=GetComponent<Collider>();
        }
        private void FixedUpdate()
        {
            if(_game==null || _game.IsReplica || _volume==null || !_volume.enabled
                || (DisableWhenReady!=null && DisableWhenReady.RegionalReady)) return;
            // CharacterController contact callbacks can omit shallow floor triggers.
            // Use the authoritative roster and the existing volume, with no allocations,
            // no client coordinates, and no duplicate damage from child colliders.
            var damage=DamagePerSecond*Time.fixedDeltaTime;
            var participants=_game.Participants;
            for(var i=0;i<participants.Count;i++)
            {
                var participant=participants[i];
                if(participant!=null && ContainsFeet(participant.transform.position)) participant.Damage(damage);
            }
            var monsters=_game.Monsters;
            for(var i=0;i<monsters.Count;i++)
            {
                var monster=monsters[i];
                if(monster!=null && ContainsFeet(monster.transform.position)) monster.EnvironmentDamage(damage);
            }
        }
        private bool ContainsFeet(Vector3 position)
        {
            var feet=position+Vector3.up*.1f;
            return (_volume.ClosestPoint(feet)-feet).sqrMagnitude<.000001f;
        }
    }
}
