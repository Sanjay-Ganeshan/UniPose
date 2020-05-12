![Put That On Logo](logo_small.png "Put That On!")

# Put That On & UniPose

Julie Ganeshan, 6.835 Final Project, Spring 2020

All the code is open-source and free to use under GNU GPLv3.
For more information (https://choosealicense.com/licenses/gpl-3.0/#)

Please credit me (by name) if you use this library!

The Pose detection is built on top of a Pytorch port of Google's PoseNet.

The Pytorch project can be found here: 
https://github.com/rwightman/posenet-pytorch
this is licensed under the Apache 2 license, provided in the directory.

My modifications are licensed under GPLv3. As is all of PutThatOn.

And Google's original PoseNet:
https://github.com/tensorflow/tfjs-models/tree/master/posenet


# Structure

*PutThatOn* is a virtual fashion app that renders and deforms 2d clothes to fit on a user in their webcam. It's controlled by gesture & voice commands.

*UniPose* is a subset of the functionality that I built for *Put That On*. UniPose allows you to quickly and easily integrate 2D Pose into a Unity project. (Using it in another python project will eventually be better supported too)



It only works on Windows, I think (I've only tried windows 10), since it uses shared memory. It also relies on Python running at the same time, so it's currently not fit for consumers, just developers. I'm working on helper scripts to make coordinating the two easier, but I don't intend to make it a standalone Unity package any time soon.

*PutThatOn* is built on top of UniPose;


# Installation

*PutThatOn* is a Unity Project. A prebuilt executable (to just run the 
application) can be found in the [Executables](Executables/) directory.
Since it's rather large, it's hosted as a zip on Google Drive. Please download it.
The Unity project itself is located in `Unity/PutThatOnV2` (V1 was 3D, built on 
Kinect, and didn't work well). UniPose must be running before *PutThatOn* is launched.

*UniPose* is a Unity package, that can be imported into any project, and a 
supporting Python script. To start 
using UniPose, simply import `Unipose.unitypackage` (in the `Unity` directory).
Note that UniPose only gives 2D pose, so it's best used in 2D Unity games. 
It relies on getting pose data from a python script, described below.

UniPose is also a Python package. The source code can be found under `Python/UniPose`. The easiest way to install the UniPose is with pip. You need Python 3.7 or higher!

```bash
pip install unipose
```

You can also download this reposity and add `unipose` somewhere where it will be found by your `PYTHONPATH`

UniPose depends on Pytorch, which **must** be installed independently. See https://pytorch.org/get-started/locally/

I **highly highly** recommend installing the GPU version of Pytorch, which will in turn need you to (a) have a compatible NVIDIA GPU, and (b) install CUDA 10.2 and CuDNN. This is a complex install which I leave to the user.

If you *do not* have a GPU, or it hasn't properly been set up, UniPose will automatically run on your CPU instead. However, it will be significantly slower, and will *not* be able to run in real-time.

At the time of writing, the Windows GPU pip installation for pytorch is as follows:

```bash
pip install torch===1.5.0 torchvision===0.6.0 -f https://download.pytorch.org/whl/torch_stable.html
```



# Quick Start - Put That On & UniPose

First, start your UniPose server (it's actually using shared memory, not network, but I'll call it the server)

```bash
# Make sure you're using the right python command for your 3.7 interpreter
# If anaconda, that's just "python". You might need "python3"
python -m unipose server
```

This will take a moment to launch. Once you see a big RECORDING icon, it's running. You can press 'q' on that window to quit, or 'p' to pause, or 's' to take a screenshot (if in drawing mode. See `-h` options for more)

Then, launch `Executables\PutThatOn.exe`

And you're ready to go!


## UniPose tools

Sometimes you don't want to always be reading from your webcam, or running the expensive neural network while debugging your code. 
For these cases, UniPose allows you to *record* the pose-frames that are being sent!

Simply start up your unipose server as usual. For demonstration, we'll draw what we're sending. You generally want to keep that option off though.
```
python -m unipose server --draw
```

Then, start a recorder (in a different terminal)!
```
python -m unipose record -f <filename>
```

This will save a compressed `.zpose` file to the provided filepath. Recording will stop when the server stops (`q` on recorder window)

Now, you can play it anytime!
```
python -m unipose play -f <filename>
```

Wondering what your Unipose server (or playback) is sending out? You can use the viewer whenever the server's running. I recommend using this only if the `--draw` command is disabled.
```
python -m unipose view
```

All of these commands are, of course, also accessible within Python scripts

```
from unipose import server, viewer, recorder, client
```

## Editing PutThatOn
To see how PutThatOn works, simply open the project folder in Unity 2019.2.8f1 (I think anything in 2019.2 is compatible). 

**Note**: The build *must* be a 32 bit Windows Standalone build to work! 64 bit builds will not support speech recognition, and other build types (i.e. Web builds) will not support shared memory access!










