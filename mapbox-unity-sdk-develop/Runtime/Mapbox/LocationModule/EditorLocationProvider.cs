using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.Attributes;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.LocationModule
{
	/// <summary>
	/// The EditorLocationProvider is responsible for providing mock location and heading data
	/// for testing purposes in the Unity editor.
	/// </summary>
	public class EditorLocationProvider : AbstractEditorLocationProvider
	{
		/// <summary>
		/// The mock "latitude, longitude" location, respresented with a string.
		/// You can search for a place using the embedded "Search" button in the inspector.
		/// This value can be changed at runtime in the inspector.
		/// </summary>
		[SerializeField]
		[Geocode]
		private string _latitudeLongitude;
		private LatitudeLongitude _latLng;
		
#if UNITY_EDITOR
		protected virtual void Start()
		{
			base.Awake();
			_latLng = Conversions.StringToLatLon(_latitudeLongitude);
		}
#endif

		protected override void SetLocation()
		{
			_currentLocation.UserHeading = transform.eulerAngles.y;
			_currentLocation.LatitudeLongitude = _latLng;
			_currentLocation.Accuracy = _accuracy;
			_currentLocation.Timestamp = UnixTimestampUtils.To(DateTime.UtcNow);
			_currentLocation.IsLocationUpdated = true;
			_currentLocation.IsUserHeadingUpdated = true;
			_currentLocation.IsLocationServiceEnabled = true;
		}
	}
}
