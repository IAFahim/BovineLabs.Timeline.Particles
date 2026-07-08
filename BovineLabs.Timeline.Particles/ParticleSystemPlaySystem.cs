#pragma warning disable CS0618 // Managed-component API (AddComponentObject/GetComponentObject/ManagedAPI) deprecated in Entities 6.6; TODO: migrate to UnityObjectRef<T>/unmanaged components.
namespace BovineLabs.Timeline.Particles
{
    using BovineLabs.Timeline.Data;
    using Data;
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
        private readonly System.Collections.Generic.HashSet<Entity> initialized = new();

        private readonly System.Collections.Generic.Dictionary<Entity, int> activeCounts = new();

        private readonly System.Collections.Generic.HashSet<Entity> played = new();

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            foreach (var binding in SystemAPI.Query<RefRO<TrackBinding>>().WithAll<ParticleClipData>())
            {
                var e = binding.ValueRO.Value;
                if (e == Entity.Null || initialized.Contains(e))
                {
                    continue;
                }

                if (TryGetParticleSystem(e, out var ps))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    initialized.Add(e);
                }
            }

            activeCounts.Clear();
            foreach (var binding in SystemAPI.Query<RefRO<TrackBinding>>().WithAll<ParticleClipData, ClipActive>())
            {
                var e = binding.ValueRO.Value;
                if (e == Entity.Null)
                {
                    continue;
                }

                activeCounts.TryGetValue(e, out var count);
                activeCounts[e] = count + 1;
            }

            foreach (var (binding, data, activePrev, clipEntity) in SystemAPI
                         .Query<RefRO<TrackBinding>, RefRO<ParticleClipData>, EnabledRefRO<ClipActivePrevious>>()
                         .WithAll<ClipActive>()
                         .WithPresent<ClipActivePrevious>()
                         .WithEntityAccess())
            {
                if (played.Contains(clipEntity))
                {
                    continue;
                }

                if (!TryGetParticleSystem(binding.ValueRO.Value, out var ps))
                {
                    if (!activePrev.ValueRO)
                    {
                        Debug.LogWarning($"ParticleSystemClip on entity {binding.ValueRO.Value} did not play yet: binding is Null or the bound GameObject has no ParticleSystem companion (will retry while active).");
                    }

                    continue;
                }

                var withChildren = data.ValueRO.PlayWithChildren;
                ps.Clear(withChildren);
                ps.Play(withChildren);
                played.Add(clipEntity);
            }

            foreach (var (binding, data, activePrev, clipEntity) in SystemAPI
                         .Query<RefRO<TrackBinding>, RefRO<ParticleClipData>, EnabledRefRO<ClipActivePrevious>>()
                         .WithDisabled<ClipActive>()
                         .WithPresent<ClipActivePrevious>()
                         .WithEntityAccess())
            {
                if (!activePrev.ValueRO)
                {
                    continue;
                }

                played.Remove(clipEntity);

                if (activeCounts.TryGetValue(binding.ValueRO.Value, out var stillActive) && stillActive > 0)
                {
                    continue;
                }

                if (!TryGetParticleSystem(binding.ValueRO.Value, out var ps))
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

            if (entity == Entity.Null || !EntityManager.HasComponent<ParticleSystem>(entity))
            {
                return false;
            }

            particleSystem = EntityManager.GetComponentObject<ParticleSystem>(entity);
            return particleSystem != null;
        }
    }
}
