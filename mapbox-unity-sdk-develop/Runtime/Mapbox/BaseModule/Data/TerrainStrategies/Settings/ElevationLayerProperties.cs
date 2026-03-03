using System;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.ImageModule.Terrain.Settings
{
	[Serializable]
	public class ElevationLayerProperties 
	{
		public ElevationModificationOptions modificationOptions = new ElevationModificationOptions();
		public UnityLayerOptions unityLayerOptions = new UnityLayerOptions();
		public TerrainSideWallOptions sideWallOptions = new TerrainSideWallOptions();
		public float TileMeshSize = 1;
	}
}
