using Godot;
using System;

namespace Orbitals;
public partial class BeamPassiveNoise : AudioStreamPlayer
{
    public void OnTractorBeamMode(int mode)
    {
        var currentBeamMode = (Beam.BeamMode)mode;

        switch (currentBeamMode)
        {
            case Beam.BeamMode.Single:
                PitchScale = 0.7f;
                break;

            case Beam.BeamMode.Multiple:
                PitchScale = 0.5f;
                break;

            default:
                Stop();
                return;
        }

        if (!Playing)
            Play();
    }
}
