using Godot;
using System;

namespace Orbitals;
public partial class CritterNoise : AudioStreamPlayer
{
	public void OnCritterCreatureDetach()
	{
		Play();
	}
}
