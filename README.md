<h1 align="center">
  ZED Unity Plugin
  <br>
</h1>

<p align="center">
This package brings the features of the <a href="https://www.stereolabs.com/store/">ZED cameras</a> into Unity. It comes with prefabs to make adding advanced features to your project as simple as drag-and-drop, helper scripts to simplify custom integration, and numerous examples to see your camera in action and learn how it works.
</p>

---

## Overview

| [Body Tracking](https://www.stereolabs.com/docs/unity/body-tracking/) | [AR/MR](https://www.stereolabs.com/docs/unity/creating-mixed-reality-app/) | [Point Cloud](https://www.stereolabs.com/docs/unity/samples/#point-cloud)
| :-----------: |  :------------: | :--------: |
| ![bt](https://user-images.githubusercontent.com/113181784/231981880-eb9a5a7e-a4e3-4dac-909d-22f5fca33342.gif) | ![planet](https://user-images.githubusercontent.com/113181784/231981947-9b07b84a-8d92-4e60-aea1-982ed13b1d66.gif) | ![pointcloud](https://user-images.githubusercontent.com/113181784/231982024-538dd3dd-58cc-4b6c-8260-3d026a0132cf.gif)
| [**Object Detection**](https://www.stereolabs.com/docs/unity/object-detection/) | [**Dark Room**](https://www.stereolabs.com/docs/unity/lighting/) | [**Spatial mapping**](https://www.stereolabs.com/docs/unity/spatial-mapping-unity/)
| ![od](https://user-images.githubusercontent.com/113181784/231982040-3275f251-435a-41e1-99ba-c16e129bdbe2.gif) | ![dark](https://user-images.githubusercontent.com/113181784/231981911-1437f38c-d974-470b-8c86-217cbeec6591.gif) | ![spamap](https://user-images.githubusercontent.com/113181784/231982066-0831e8c0-0700-429b-8169-4d7b9d25d75d.gif)
| [**Object Placement**](https://www.stereolabs.com/docs/unity/object-placement/) | [**ArUco Markers**](https://www.stereolabs.com/docs/unity/using-opencv-with-unity/) | [**Green Screen**](https://www.stereolabs.com/docs/unity/green-screen-vr/)
| ![placement](https://user-images.githubusercontent.com/113181784/231982097-c1013a5c-2b65-4c63-8d1a-5cb525335044.gif) | ![aru](https://user-images.githubusercontent.com/113181784/231982111-477485bd-f135-4f68-a4d8-2feef8d467ec.gif) | ![green](https://user-images.githubusercontent.com/113181784/231982130-81b7f0bf-6c72-4435-b6fc-8b77494df366.gif)

## Compatibility

<div align="center">

| ZED Camera | GPU | AR/VR |
| :------: | :-----------------------: | :---------------: |
| <div align="center">Any <a href="https://store.stereolabs.com/">ZED camera</a></div>  | <div align="center">CUDA&nbsp;10.2 capability or higher<br> GTX&nbsp;1060 or higher is recommended</div> | <div align="center"><div><span><a href="https://github.com/ValveSoftware/openvr"><img src="https://user-images.githubusercontent.com/113181784/231974244-37054070-9a80-4f1e-ad8f-30715c2faab8.jpg" width="20%" alt="" /></a></span><span><a href="https://developer.oculus.com/downloads/unity/"><img src="https://user-images.githubusercontent.com/113181784/232449062-ac1ee35c-d4d3-4a1b-9141-cc80caf54d14.jpg" width="20%" alt="" /></a></span></div><div>HMDs compatible with OpenVR or Oculus</div></div>
| <div align="center">**OS**</div>  | <div align="center">**Unity Version**</div> | <div align="center">**ZED SDK Version**</div>
| <div align="center">Windows 10/11 <br> Ubuntu 20/22</div>  | <div align="center"><a href="https://unity.com/download">Unity 2021.3</a> and newer</div> | <div align="center"><a href="https://www.stereolabs.com/developers/release/">ZED SDK **4.1**</a><br><em>Previous versions are supported by <a href="https://github.com/stereolabs/zed-unity/releases">previous plugins</a></em></div>

</div>
  
## Get Started

To develop applications in Unity with your ZED, you'll need the following things:

- Download and install the [latest ZED SDK](https://www.stereolabs.com/developers/release/).
- Install the ZED SDK plugin through Unity's package manager:
  - Navigate to Window -> Package Manager in the Editor
  - Click the "+" button at the top left and select "Add package from git URL..."
  - Copy this URL into the field and hit Add:
```
https://github.com/stereolabs/zed-unity.git?path=/ZEDCamera/Assets
```
  - Wait for the package to install (it can take some time, depending on your connection)
- Install the samples you want to try out/use through the Package Manager
  - Select the "ZED SDK" package
  - Go to the "Samples" tab and import the samples you want.
  - Consider importing the Assembly Definition, it potentially improves the compile time and editor responsiveness at no cost.
- Read the [Basic Concepts](https://www.stereolabs.com/docs/unity/basic-concepts/) and [Build Your First AR/MR App](https://www.stereolabs.com/docs/unity/creating-mixed-reality-app/) guides to get started.
- Explore the [Samples](https://www.stereolabs.com/docs/unity/samples/) included in the plugin.
- The [Main Scripts](https://www.stereolabs.com/docs/unity/main-scripts/) page of the documentation introduces the important scripts of the plugin and their parameters.

> **Note**: See how to use **URP and HDRP** with ZED [on the documentation](https://www.stereolabs.com/docs/unity#step-1-installation).

## Example Scenes

After importing the plugin, pick and try some of the example scenes. Each is designed to demonstrate a main feature of the ZED. Some contain prefabs and example scripts that can be repurposed for your projects.

The overview is available in the [documentation for the ZED Unity plugin](https://www.stereolabs.com/docs/unity/samples/).

- [Body Tracking](https://www.stereolabs.com/docs/unity/body-tracking/): Animate a 3D avatar based on real-people movements using the ZED SDK [Body Tracking](https://www.stereolabs.com/docs/body-tracking/) module.
- [Dark Room](https://www.stereolabs.com/docs/unity/lighting/): Your office is now a nightclub! Explore the lighting features coupled to the depth sensing to cast a laser show on your walls.
- [Drone Shooter](https://www.stereolabs.com/docs/unity/samples/#drone-battle): Defend yourself from drones that spawn around your room and shoot at you. Block lasers with your hand, and shoot back with the spacebar, or VR controllers if using the Oculus Integration or SteamVR plugin.
- [Green Screen](https://www.stereolabs.com/docs/unity/green-screen-vr/): Aim your ZED at a greenscreen and hit Play to see your subject standing in a small town in the desert. You’ll see that the nearby crates still have all the proper occlusion, but the greenscreen background is replaced with the virtual background.
- [Movie Screen](https://www.stereolabs.com/docs/unity/samples/#movie-screen): Simple AR sample, playing a movie on a movable, scalable 3D screen integrated to the real-world. Demonstrates how 2D content can easily be displayed in a 3D, mixed-reality scene.
- [Multicam](./ZEDCamera/Assets/ZED/Examples/MultiCam/): Integrate data from multiple ZED cameras in the same scene, at the same time.
- [Object Detection](https://www.stereolabs.com/docs/unity/object-detection/): Detect objects and people bounding boxes using the ZED SDK Object Detection module.
- [Object Placement](https://www.stereolabs.com/docs/unity/object-placement/): Place virtual objects on real-world surfaces using the [Plane Detection](https://www.stereolabs.com/docs/spatial-mapping/plane-detection/) capabilities of the ZED SDK.
- [OpenCV ArUco Detection](https://www.stereolabs.com/docs/unity/using-opencv-with-unity/): Print out ArUco markers, put them in view of your ZED, and let the battle of the drones begin. Shows how to easily interface the ZED with OpenCV for marker detection using a variety of included scripts.
- [Plane Detection](https://www.stereolabs.com/docs/unity/samples/#simple-plane-detection): Run the scene and hold down spacebar to see if you’re looking at a valid surface where a bunny could stand. Release the spacebar and a bunny will fall from the sky and land on that surface with proper physics.
- [VR Plane Detection](https://www.stereolabs.com/docs/unity/samples/#vr-only-plane-detection-requires-vr-hmd-and-oculussteamvr-plugin): Throw a plushie as far as possible, the distance is measured where it lands using the Plane Detection. Shows how plane detection can fit into a proper VR game.
- [Planetarium](https://www.stereolabs.com/docs/unity/samples/#planetarium): A beautiful display of the ZED plugin’s basic mixed reality features, viewable with or without a headset. Watch as the planets are properly occluded by the real world.
- [Point Cloud](https://www.stereolabs.com/docs/unity/samples/#point-cloud): Visualize the depth retrieved by the ZED Camera directly into Unity, in the form of a Point Cloud.
- [Simple MR](https://www.stereolabs.com/docs/unity/creating-mixed-reality-app/): Learn to create a basic AR app, with depth occlusion and camera tracking.
- [Spatial Mapping](https://www.stereolabs.com/docs/unity/spatial-mapping-unity/): Capture a mesh of your environment, allowing physical interactions between the real and virtual world.

## Additional Resources

Want more details? Our [📖 Documentation](https://www.stereolabs.com/docs/unity/) has overviews and tutorials for getting started with specific features.

Got a problem you just can't seem to solve? Got a project you want to share? Check out our [Community forums](https://community.stereolabs.com/)!

## Bugs and fixes

You found a bug / a flaw in our plugin ? Please check that it is not [already reported](https://github.com/stereolabs/zed-unity/issues), and open an issue if necessary. You can also reach out to us on the community forums for any question or feedback !

*By the way, we also have a special place in our hearts for PR senders.*
