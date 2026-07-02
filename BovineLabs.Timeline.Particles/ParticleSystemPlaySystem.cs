namespace BovineLabs.Timeline.Particles
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Particles.Data;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Drives <c>ParticleSystemClip</c>. The bound <see cref="ParticleSystem"/> lives on a companion GameObject
    /// (Entities.Graphics' <c>ParticleSystemCompanionBaker</c>), reachable from the clip's <c>TrackBinding</c> entity
    /// via <c>EntityManager.GetComponentObject</c>. On a clip's rising edge (active this frame, inactive last frame)
    /// the system Clears and Plays the particle system from the start; on its falling edge it Stops it. Managed
    /// (calls the ParticleSystem API), so it runs on the main thread, not Burst.
    ///
    /// Runs in <see cref="TimelineComponentAnimationGroup"/> before <c>ClipActivePreviousSystem</c> records the new
    /// state, mirroring the edge pattern used by the Audio Impact Stinger and Essence event tracks.
    /// </summary>
    [UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial class ParticleSystemPlaySystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // Rising edge: clip active this frame, inactive last frame -> restart from the start.
            foreach (var (binding, data, activePrev) in SystemAPI
                         .Query<RefRO<TrackBinding>, RefRO<ParticleClipData>, EnabledRefRO<ClipActivePrevious>>()
                         .WithAll<ClipActive>()
                         .WithPresent<ClipActivePrevious>())
            {
                if (activePrev.ValueRO)
                {
                    continue; // already active last frame, not the start edge
                }

                if (!this.TryGetParticleSystem(binding.ValueRO.Value, out var ps))
                {
                    Debug.LogWarning($"ParticleSystemClip on entity {binding.ValueRO.Value} did not play: binding is Null or the bound GameObject has no ParticleSystem companion.");
                    continue;
                }

                var withChildren = data.ValueRO.PlayWithChildren;
                ps.Clear(withChildren);
                ps.Play(withChildren);
            }

            // Falling edge: clip inactive this frame, active last frame -> stop.
            foreach (var (binding, data, activePrev) in SystemAPI
                         .Query<RefRO<TrackBinding>, RefRO<ParticleClipData>, EnabledRefRO<ClipActivePrevious>>()
                         .WithDisabled<ClipActive>()
                         .WithPresent<ClipActivePrevious>())
            {
                if (!activePrev.ValueRO)
                {
                    continue; // wasn't active last frame, nothing to stop
                }

                if (!this.TryGetParticleSystem(binding.ValueRO.Value, out var ps))
                {
                    continue;
                }

                var withChildren = data.ValueRO.PlayWithChildren;
                ps.Stop(
                    withChildren,
                    data.ValueRO.StopAndClear
                        ? ParticleSystemStopBehavior.StopEmittingAndClear
                        : ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private bool TryGetParticleSystem(Entity entity, out ParticleSystem particleSystem)
        {
            particleSystem = null;

            if (entity == Entity.Null || !this.EntityManager.HasComponent<ParticleSystem>(entity))
            {
                return false;
            }

            particleSystem = this.EntityManager.GetComponentObject<ParticleSystem>(entity);
            return particleSystem != null;
        }
    }
}
