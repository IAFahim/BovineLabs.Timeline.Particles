namespace BovineLabs.Timeline.Particles.Authoring
{
    using BovineLabs.Timeline.Authoring;
    using BovineLabs.Timeline.Particles.Data;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Plays the bound <see cref="ParticleSystem"/> for the clip's duration. <c>ParticleSystemPlaySystem</c> restarts
    /// it from the start on the clip's start edge (Clear + Play) and stops it on the end. A finished system is never
    /// re-shown stale — re-entry replays from the start.
    /// </summary>
    public sealed class ParticleSystemClip : DOTSClip, ITimelineClipAsset
    {
        [Tooltip("Include child particle systems when playing/stopping (use for composite effects built from several systems).")]
        public bool playWithChildren = true;

        [Tooltip("On the clip end, also clear live particles (true) or just stop emitting and let existing particles finish their lifetime (false).")]
        public bool stopAndClear;

        /// <inheritdoc/>
        public override double duration => 1;

        // Edge-driven restart/stop; no Looping (a baked edge would not re-fire a looped clip).
        public ClipCaps clipCaps => ClipCaps.Blending;

        /// <inheritdoc/>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            context.Baker.AddComponent(clipEntity, new ParticleClipData
            {
                PlayWithChildren = this.playWithChildren,
                StopAndClear = this.stopAndClear,
            });

            base.Bake(clipEntity, context);
        }
    }
}
