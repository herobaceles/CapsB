using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.UnityMapService.TileProviders
{
	[Serializable]
	public class UnityTileProviderSettings
	{
		public Camera Camera;
		public float MinimumZoomLevel = 2;
		public float MaximumZoomLevel = 22;

		public UnityTileProviderSettings(Camera cam, float minZoom = 2, float maxZoom = 22)
		{
			Camera = cam;
			MinimumZoomLevel = minZoom;
			MaximumZoomLevel = maxZoom;
		}
	}
	
    public class UnityTileProvider : TileProvider
    {
	    public UnityTileProviderSettings Settings;
        private float _quadMaxLevel;
        private Plane[] _planes = new Plane[6];
        private Stack<UnityRectD> _stack;
        private List<UnityRectD> _rectpool;
        private int _rectIndex = 0;

        public UnityTileProvider(UnityTileProviderSettings settings)
        {
	        Settings = settings;
	        
	        if(Settings.Camera == null) Settings.Camera = Camera.main;
	        
	        _rectpool = new List<UnityRectD>(200);
	        for (int i = 0; i < 200; i++)
	        {
		        _rectpool.Add(new UnityRectD());
	        }
	        _stack = new Stack<UnityRectD>(200);
        }

        private UnityRectD GetRectD()
        {
	        if(_rectIndex >= _rectpool.Count)
		        _rectpool.Add(new UnityRectD());
	        var value = _rectpool[_rectIndex];
	        _rectIndex++;
	        return value;
        }
        
        public override bool GetTileCover(IMapInformation mapInformation, TileCover tileCover)
        {
	        _rectIndex = 0;
            tileCover.Tiles.Clear();
            _quadMaxLevel = Mathf.Min(Settings.MaximumZoomLevel, mapInformation.AbsoluteZoom);
            GeometryUtility.CalculateFrustumPlanes(Settings.Camera, _planes);
            var worldBaseBounds = GetRectD();
	        worldBaseBounds.Set(new UnwrappedTileId(0, 0, 0), -mapInformation.CenterMercator, mapInformation.Scale, 0);
	        _stack.Clear();
	        _stack.Push(worldBaseBounds);
            while (_stack.Count > 0)
            {
                var tile = _stack.Pop();
                if (!GeometryUtility.TestPlanesAABB(_planes, tile.UnityBounds))
                {
                    continue;
                }

                if (tile.Id.Z == _quadMaxLevel || !ShouldSplit(tile.Id.Z, tile.UnityBounds.size.z, tile.UnityBounds.SqrDistance(Settings.Camera.transform.position)))
                {
                    tileCover.Tiles.Add(tile.Id);
                    continue;
                }

                for (var i = 0; i < 4; i++)
                {
                    var child  = tile.Quadrant(GetRectD(), i);
                    _stack.Push(child);
                }
            }

            return true;
        }

        private bool ShouldSplit(int zoom, float sizeX, float dist)
        {
            if (zoom < Settings.MinimumZoomLevel)
            {
                return true;
            }
            else if (zoom == _quadMaxLevel)
            {
                return false;
            }

            return dist < Mathf.Pow(sizeX , 2);
        }
        
        private struct UnityRectD
        {
        	public UnwrappedTileId Id;
        	public Bounds UnityBounds;
    
        	private Vector2d _offset;
        	private float _worldScale;
        	private float _currentElevationSample;
        	const int TileSize = 256;
        	
        	public void Set(UnwrappedTileId id, Vector2d vector2d, float worldScale, float unityBoundHeight = 1)
        	{
        		_currentElevationSample = unityBoundHeight;
        		_offset = vector2d;
        		_worldScale = worldScale;
        		Id = id;
        		
        		var min = Conversions.PixelsToMeters(
        			Id.X * TileSize,
        			Id.Y * TileSize,
        			Id.Z);
        		var max = Conversions.PixelsToMeters(
        			(Id.X + 1) * TileSize,
        			(Id.Y + 1) * TileSize,
        			Id.Z);
        		
        		UnityFlatSpaceCalculations(
        			(float)min.x + vector2d.x,
        			(float)min.y + vector2d.y,
        			(float)(max.x - min.x),
        			worldScale, _currentElevationSample);
        	}
        	
        	private void UnityFlatSpaceCalculations(double boundX, double boundY, double boundZ, float worldScale, float boundHeight)
        	{
        		//var boundsTopLeft = bounds.TopLeft;
        		var topleftX = (float) (boundX / worldScale);
        		var toplefty = (float) (boundY / worldScale);
        		var boundsSize = (float)(boundZ / worldScale);
    
        		UnityBounds = new Bounds(
        			new Vector3(topleftX + boundsSize / 2, boundHeight/2, toplefty - boundsSize / 2),
        			new Vector3(
        				boundsSize,
        				boundHeight,
        				boundsSize));
        	}
    
        	public UnityRectD Quadrant(UnityRectD rectD, int i)
        	{
        		var childX  = (Id.X << 1) + (i % 2);
        		var childY  = (Id.Y << 1) + (i >> 1);

                rectD.Set(new UnwrappedTileId(Id.Z + 1, childX, childY), _offset, _worldScale, _currentElevationSample);
                return rectD;
            }
    
        	public UnwrappedTileId QuadrantTileId(int i)
        	{
        		var childX  = (Id.X << 1) + (i % 2);
        		var childY  = (Id.Y << 1) + (i >> 1);
    
        		return new UnwrappedTileId(Id.Z + 1, childX, childY);
        	}
        }
    }
}