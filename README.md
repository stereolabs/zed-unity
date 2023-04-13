# Stereolabs ZED - Unity plugin

This package brings the many mixed reality features of the [ZED](https://www.stereolabs.com/zed/), [ZED Mini](https://www.stereolabs.com/zed-mini/) and [ZED 2](https://www.stereolabs.com/zed-2/) depth sensors into Unity. It comes with prefabs to make adding advanced features to your project as simple as drag-and-drop, helper scripts to simplify custom integration, and numerous examples to see the ZED/ZED Mini in action and learn how it works.

Features include:
 - Body tracking
 - Pass-through augmented reality with an Oculus Rift, HTC Vive and other headsets
 - Third person mixed reality
 - Real-time depth occlusion between the real and virtual
 - Adding virtual lights and shadows to real objects
 - Inside-out positional tracking
 - Spatial mapping
 - Real-time plane detection
 - Object detection

## Overview

| [Body Tracking](https://www.stereolabs.com/docs/unity/body-tracking/) | [AR/MR](https://www.stereolabs.com/docs/unity/creating-mixed-reality-app/) | [Point Cloud](https://www.stereolabs.com/docs/unity/samples/#point-cloud)
| :-----------: |  :------------: | :--------: |
| <video src="https://user-images.githubusercontent.com/113181784/202238672-b87ec681-a574-454c-ba09-683201d1dcbb.mp4" controls="controls" style="max-width: 100;"></video> | <video src="https://user-images.githubusercontent.com/113181784/202200233-ff0a6777-ab02-4896-aad5-8904b9318443.mp4"></video> | <video src="https://user-images.githubusercontent.com/113181784/202454461-5ecb0b60-e518-4d50-aba1-7cef4699d124.mp4"></video>
| [**Object Detection**](https://www.stereolabs.com/docs/unity/object-detection/) | [**Dark Room**](https://www.stereolabs.com/docs/unity/lighting/) | [**Spatial mapping**](https://www.stereolabs.com/docs/unity/spatial-mapping-unity/)
| <video src="https://thumbs.gfycat.com/DiligentWindingGrayreefshark-mobile.mp4" ></video> | <video src="https://user-images.githubusercontent.com/113181784/202200729-966c34de-654e-4c4c-a977-941af3c854b7.mp4"></video> | <video src="https://thumbs.gfycat.com/ReflectingAgedAphid-mobile.mp4"></video>
| [**Object Placement**](https://www.stereolabs.com/docs/unity/object-placement/) | [**ArUco Markers**](https://www.stereolabs.com/docs/unity/using-opencv-with-unity/) | [**Green Screen**](https://www.stereolabs.com/docs/unity/green-screen-vr/)
| <video src="https://thumbs.gfycat.com/RepentantFamousAndeancondor-mobile.mp4" ></video> | <video src="https://user-images.githubusercontent.com/113181784/202201640-ff353944-0adb-4eba-a672-564d5d475f97.mp4"></video> | <video src="https://thumbs.gfycat.com/PalatableDesertedHorse-mobile.mp4"></video>

## About Fusion

To use the **Fusion API** of the ZED SDK with Unity, please take a look at our [ZED Live Link for Unity](https://github.com/stereolabs/zed-unity-livelink) repository. The Fusion API is currently not implemented in the Unity plugin itself, but it can still be easily integrated and tested in your Unity projects!

## Compatibility

Make sure you have the following:

**Hardware:**
 - ZED, ZED-M or ZED2 camera *(ZED Mini required for HMD applications)*
 - NVIDIA GPU with CUDA capability 3.0 or higher (a GTX 1060 or higher is recommended for AR pass-through)
 - Dual core processor clocked at 2.3GHz or higher
 - 4GB RAM or more
 - USB 3.0
 - Compatible VR headset *(required for pass-through AR)*

Supported VR headsets:
- Oculus Rift
- HTC Vive
- HTC Vive Pro*
- Windows Mixed Reality headset* (via SteamVR only)

**Stereolabs' ZED Mini mounting bracket was designed for the Oculus Rift and original HTC Vive. It can fit on some WMR headsets like the Samsung Odyssey, but you will need to create a custom attachment for the Vive Pro or other WMR headsets.*

**Software:**
 - Windows 7-11, Ubuntu 16-22
 - NVIDIA CUDA† ([download](https://developer.nvidia.com/cuda-downloads))
 - Unity 2018.1 or above ([download](https://unity3d.com/get-unity/download/archive))
 - ZED SDK ([download](https://www.stereolabs.com/developers/release/latest/))
 - ZED Unity plugin ([download](https://github.com/stereolabs/zed-unity/releases))


† *A Linux version of the plugin is available in the Linux_compatibility_beta branch for ZED SDK 2.8.
Since SDK 3.0, Windows and Linux version are in the same default branch.
See the tutorial (https://github.com/stereolabs/zed-unity/tree/master/ZEDCamera/Assets/ZED/Doc/Tutorials)*

## Get Started

To develop applications in Unity with your ZED, you'll need the following things:

- Download and install the [ZED SDK](https://www.stereolabs.com/developers/release/).
- Download the [ZED Plugin for Unity](https://github.com/stereolabs/zed-unity/releases).
- Import the *ZEDCamera.package* into Unity.
- Read the [Basic Concepts](https://www.stereolabs.com/docs/unity/basic-concepts/) and [Build Your First AR/MR App](https://www.stereolabs.com/docs/unity/creating-mixed-reality-app/) guides to get started.
- Explore the [Samples](https://www.stereolabs.com/docs/unity/samples/) included in the plugin.
- The [Main Scripts](https://www.stereolabs.com/docs/unity/main-scripts/) page of the documentation introduces the important scripts of the plugin and their parameters.

## Example Scenes

After importing the plugin, pick and try some of the example scenes. Each is designed to demonstrate a main feature of the ZED. Some contain prefabs and example scripts that can be repurposed for your projects.

The overview is available in the [documentation for the ZED Unity plugin](https://www.stereolabs.com/docs/unity/samples/).

## Additional Resources

Want more details? Our [Documentation](https://www.stereolabs.com/docs/unity/) has overviews and tutorials for getting started with specific features.

Not sure where to start with attaching your ZED Mini to your VR headset? See our guides for [Oculus Rift](https://www.stereolabs.com/zed-mini/setup/rift/) and [HTC Vive](https://www.stereolabs.com/zed-mini/setup/vive/).

Got a problem you just can't seem to solve? Check our [Community forums](https://community.stereolabs.com/) or contact our support at [support@stereolabs.com](mailto:support@stereolabs.com).

## Bugs and fixes

You found a bug / a flaw in our plugin ? Please check that it is not [already reported](https://github.com/stereolabs/zed-unity/issues), and open an issue if necessary. You can also reach out to us on the community forums for any question or feedback !

*By the way, we also have a special place in our hearts for PR senders.*