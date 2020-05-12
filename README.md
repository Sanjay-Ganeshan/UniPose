# Put That On & UniPose

Julie Ganeshan, 6.835 Final Project, Spring 2020

All the code is open-source and free to use under GNU GPLv3.
For more information (https://choosealicense.com/licenses/gpl-3.0/#)

Please credit me (by name) if you use this library!

The Pose detection is built on top of a Pytorch port of Google's PoseNet.

The Pytorch project can be found here: 
https://github.com/rwightman/posenet-pytorch
this is licensed under the Apache 2 license, provided in the directory.

My modifications are licensed under GPLv3.

And Google's original PoseNet:
https://github.com/tensorflow/tfjs-models/tree/master/posenet


# Structure

*PutThatOn* is a virtual fashion app that renders and deforms 2d clothes to fit on a user in their webcam. It's controlled by gesture & voice commands.

*UniPose* is a subset of the functionality that I built for *Put That On*. UniPose allows you to quickly and easily integrate 2D Pose into a Unity project. (Using it in another python project will eventually be better supported too)
It only works on Windows, I think (I've only tried windows 10), since it uses shared memory. It also relies on Python running at the same time, so it's currently not fit for consumers, just developers. I'm working on helper scripts to make coordinating the two easier, but I don't intend to make it a standalone Unity package any time soon.

*PutThatOn* is built on top of UniPose;


# Installation & Quick Start

*PutThatOn* is a Unity Project. A prebuilt executable (to just run the 
application) can be found in the [Executables](Executables/PutThatOn.exe).
The Unity project itself is located in `Unity/PutThatOnV2` (V1 was 3D, built on 
Kinect, and didn't work well). UniPose must be running before *PutThatOn* is launched.

*UniPose* is a Unity package, that can be imported into any project, and a 
supporting Python script. To start 
using UniPose, simply import `Unipose.unitypackage` (in the `Unity` directory).
Note that UniPose only gives 2D pose, so it's best used in 2D Unity games. 
It relies on getting pose data from a python script, described below.

UniPose is also a Python package. The source code can be found under `Python/UniPose`. The easiest way to install the UniPose is with pip:

`pip install julai-unipose`





