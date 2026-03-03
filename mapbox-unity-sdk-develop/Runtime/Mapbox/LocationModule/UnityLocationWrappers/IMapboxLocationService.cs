using UnityEngine;

namespace Mapbox.LocationModule.UnityLocationWrappers
{
	public interface IMapboxLocationService
	{


		bool isEnabledByUser { get; }

		LocationServiceStatus status { get; }

		IMapboxLocationInfo lastData { get; }

		void Start(float desiredAccuracyInMeters, float updateDistanceInMeters);

		void Stop();
	}
}
