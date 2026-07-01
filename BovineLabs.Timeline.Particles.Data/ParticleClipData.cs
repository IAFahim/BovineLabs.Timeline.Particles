namespace BovineLabs.Timeline.Particles.Data
{
    using Unity.Entities;

    /// <summary>
    /// Baked from a <c>ParticleSystemClip</c>. Carries how the clip should play/stop the bound
    /// <see cref="UnityEngine.ParticleSystem"/> companion. Read by the managed <c>ParticleSystemPlaySystem</c>.
    /// </summary>
    public struct ParticleClipData : IComponentData
    {
        /// <summary>Include child particle systems when playing/clearing/stopping (composite effects).</summary>
        public bool PlayWithChildren;

        /// <summary>On the clip end: also clear live particles (true) or just stop emitting and let them finish (false).</summary>
        public bool StopAndClear;
    }
}
