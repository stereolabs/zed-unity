How it works: Movie Screen Demo

Required:
ZED_Rig_Stereo prefab.
or
VR Hmd with 2 Controllers (Oculus or Vive).

How to use:
Either use the keyboard with the following keys:
W & S: Moving Forward & Backward
A & D: Moving Left & Right
R & F: Moving Up & Down
Up & Down Arrows: Moving Back & Forword
Left & Right Arrows: Rotating Left & Right
Mouse Left & Right Click: Scaling Up & Down

Or use VR Controllers: (Must have Oculus Integration or SteamVR plugin installed)
Left Controller Touch-Pad: Moving Forward, Backwards, Left & Right.
Right Controller Touch-Pad: Moving Up and Down, & Rotating Left & Right.
Left Controller Trigger Button: Scale Up.
Right Controller Trigger Button: Scale Down.

The Script TransformController.cs has a checkbox "VR Controls" for enabling the controls for the VR Controllers.
It also detects automatically if there are any controller connected and sets itselfs true or false.
///////////////////////////////////////////////////
......................................................
We are using Unity's Video Player to play videos. 
This component accepts video files within the project, web or local PC URL (link / path) towards the video source.

A Light appears behind the Movie Screen when its being moved around.

......................................................
The script TransformController.cs handles the movement of any object it is attributed to. 
It takes the Input form either your PC Keyboard, or your VR Controllers (Oculus or Vive), and applies it to the Object's Transform component.
It can Move it on its axis, Rotate on the Y axis, and Scale uniformly.