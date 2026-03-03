
# Getting Started with Mapbox Map Script and Object

In this guide, you’ll learn how to set up and interact with the Mapbox Map object in Unity.  
We’ll explore how the map system works, how the related scripts are structured, and how to properly access the `MapboxMap` instance in your own code.

---

## Understanding the `MapboxMap` Object

The `MapboxMap` class is the root object that controls the map instance.  
It’s a plain C# class — meaning it does not inherit from Unity’s `MonoBehaviour`.  

Because of this, you cannot attach it directly to a GameObject or interact with it in the Unity Editor.

---

## Exposing the Map in Unity: `MapboxMapBehaviour`

To expose `MapboxMap` in Unity, a helper interface class is used: `MapboxMapBehaviour`.  

This class:
- Inherits from `MapBehaviourCore`,  
- Which in turn inherits from `MonoBehaviour`.

That means `MapboxMapBehaviour` can be attached to a Unity GameObject and used directly in the Unity Scene or Inspector.

---

## How It Works

The classes `MapBehaviourCore` and `MapboxMapBehaviour` are responsible for:

1. Creating a `MapboxMap` instance using the settings configured in the Unity Inspector.  
2. Exposing the map object after it’s created, allowing other scripts to access it later.

After initialization, these classes mainly serve as a reference to retrieve the existing `MapboxMap` instance.

---

## In the Scene

When you open a demo scene, you’ll notice a GameObject named `Map`.  
This object has a `MapboxMapBehaviour` script attached.

During scene startup (in Unity’s `Start()` lifecycle method), this script automatically:
- Creates the `MapboxMap` object,
- Initializes it based on your scene’s configuration,
- Makes it available through the property:  

```csharp
MapboxMapBehaviour.MapboxMap
```

---

## The Initialization Process

By default, map initialization starts during Unity’s built-in `Start()` phase.

However, you can change this behavior — for example, to manually control when the map initializes — by adjusting the initialization settings in the Inspector.

---

## Creating a Basic Map Setup

To create a basic map in your own Unity scene, follow these simple steps:

1. Create a new GameObject and add the `MapboxMapBehaviour` script.  
   This script is the core of the map system—it represents the map itself. However, it does not include any default visualization, so we’ll add a few modules to display it as desired.

2. Add a `StaticApiLayerModule` script to the same GameObject.  
   The Static API module works with image data, downloading server-rendered map images and draping them over the terrain (flat or elevated).  
   It includes several predefined style options, and you can also use your own custom styles created in Mapbox Studio.  
   Without changing any settings, running the application should display a basic map.  
   (Remember to adjust the Scene or Game view camera so the map is visible.)

3. Add a `TerrainLayerModule` script to the same GameObject.  
   The Terrain module also works with image data, but instead of draping it as imagery, it extracts elevation information and applies it to the base terrain mesh.  
   Running the application again with default settings will display small elevation details and terrain variation.

4. Add a `VectorLayerModule` script to the same GameObject.  
   The Vector module is different—it works with vector data rather than images.  
   By default, it uses the Mapbox `Streets-v8` tileset, which you can read more about here: [Mapbox Streets V8 Documentation](https://docs.mapbox.com/data/tilesets/reference/mapbox-streets-v8/).  
   Unlike other modules, the Vector module doesn’t create visuals on its own. If you run the map now, you won’t see any new features appear yet.  
   The Vector module needs subcomponents called `Layer Visualizers` to turn data into visuals. You can think of these as “styles” that define how vector data appears.  
   If you click the `+` button and add a `BuildingLayer` visualizer (available in the sample content), then run the application, you’ll see basic buildings appear on your map.


---
