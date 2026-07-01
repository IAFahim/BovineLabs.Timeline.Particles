namespace BovineLabs.Timeline.Particles.Authoring
{
    using System;
    using System.ComponentModel;
    using BovineLabs.Timeline.Authoring;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Plays a Unity <see cref="ParticleSystem"/> (Shuriken) from a DOTS Timeline. Bind the track to a
    /// <see cref="ParticleSystem"/> on a baked GameObject (it becomes a companion entity via Entities.Graphics'
    /// ParticleSystemCompanionBaker). Each <see cref="ParticleSystemClip"/> restarts the system from the start on its
    /// start edge and stops it on its end — so re-entering a clip replays the effect (including composite child
    /// systems), unlike the GameObjects Activation track which only un-hides a finished particle system.
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(ParticleSystemClip))]
    [TrackColor(0.85f, 0.5f, 0.2f)]
    [TrackBindingType(typeof(ParticleSystem))]
    [DisplayName("BovineLabs/Timeline/Particles/Play Particle System")]
    public sealed class ParticleSystemTrack : DOTSTrack
    {
    }
}
