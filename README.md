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
| <div align="center">Windows 10/11 <br> Ubuntu 18-22</div>  | <div align="center"><a href="https://unity.com/download">Unity 2021.3</a> and newer</div> | <div align="center"><a href="https://www.stereolabs.com/developers/release/">ZED SDK 4.0</a><br><em>Previous versions are supported by <a href="https://github.com/stereolabs/zed-unity/releases">previous plugins</a></em></div>

</div>
  
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

Want more details? Our [📖 Documentation](https://www.stereolabs.com/docs/unity/) has overviews and tutorials for getting started with specific features.

Got a problem you just can't seem to solve? Got a project you want to share? Check out our [Community forums](https://community.stereolabs.com/)!

## Bugs and fixes

You found a bug / a flaw in our plugin ? Please check that it is not [already reported](https://github.com/stereolabs/zed-unity/issues), and open an issue if necessary. You can also reach out to us on the community forums for any question or feedback !

*By the way, we also have a special place in our hearts for PR senders.*
