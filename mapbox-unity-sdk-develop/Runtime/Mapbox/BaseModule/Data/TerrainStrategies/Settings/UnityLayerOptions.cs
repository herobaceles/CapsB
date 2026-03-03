using System;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	[Serializable]
	public class UnityLayerOptions
	{
		[Tooltip("Add terrain tiles to Unity Layer")]
		public bool addToLayer = false;
		[Tooltip("Unity Layer id to which terrain tiles will get added.")]
		public int layerId = 0;
	}
}
