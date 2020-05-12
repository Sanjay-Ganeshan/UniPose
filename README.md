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


# Installation

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

`pip install unipose`

You can also download this reposity and add `unipose` somewhere where it will be found by your `PYTHONPATH`

UniPose depends on Pytorch, which **must** be installed independently. See https://pytorch.org/get-started/locally/

I **highly highly** recommend installing the GPU version of Pytorch, which will in turn need you to (a) have a compatible NVIDIA GPU, and (b) install CUDA 10.2 and CuDNN. This is a complex install which I leave to the user.

If you *do not* have a GPU, UniPose will automatically run on your CPU instead. However, it will be slower, and will *not* be able to run in real-time.


# Quick Start - Put That On & UniPose

First, start your UniPose server (it's actually using shared memory, not network, but I'll call it the server)

`python -m unipose server`

This will take a moment to launch. Once you see a big RECORDING icon, it's running. You can press 'q' on that window to quit, or 'p' to pause, or 's' to take a screenshot (if in drawing mode. See `-h` options for more)

Then, launch `Executables\PutThatOn.exe`

And you're ready to go!


## UniPose tools

Sometimes you don't want to always be reading from your webcam, or running the expensive neural network while debugging your code. 
For these cases, UniPose allows you to *record* the pose-frames that are being sent!

Simply start up your unipose server as usual. For demonstration, we'll draw what we're sending. You generally want to keep that option off though.
`python -m unipose server --draw`

Then, start a recorder (in a different terminal)!
`python -m unipose record -f <filename>`

This will save a compressed `.zpose` file to the provided filepath. Recording will stop when the server stops (`q` on recorder window)

Now, you can play it anytime!
`python -m unipose play -f <filename>`

Wondering what your Unipose server (or playback) is sending out? You can use the viewer whenever the server's running. I recommend using this only if the `--draw` command is disabled.
`python -m unipose view`












