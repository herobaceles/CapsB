using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.Example.Scripts.MapInput
{
	public abstract class MapInput
	{
		protected Camera _camera;
		public abstract bool UpdateCamera(IMapInformation mapInfo);
	}
}