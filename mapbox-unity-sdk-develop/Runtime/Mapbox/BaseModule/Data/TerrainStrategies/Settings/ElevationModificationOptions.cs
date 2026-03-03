using System;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	[Serializable]
	public class ElevationModificationOptions
	{
		[Tooltip("Results in 128/n x 128/n grid")]
		[Range(1,128)]
		public int SimplificationFactor = 1;
		public int sampleCount => 128 / SimplificationFactor;
	}
}
