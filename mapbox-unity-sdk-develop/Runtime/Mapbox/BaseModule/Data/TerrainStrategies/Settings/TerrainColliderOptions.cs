using System;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	[Serializable]
	public class TerrainColliderOptions 
	{
		[Tooltip("Add Unity Physics collider to terrain tiles, used for detecting collisions etc.")]
		public bool addCollider = false;

		
	}
}
