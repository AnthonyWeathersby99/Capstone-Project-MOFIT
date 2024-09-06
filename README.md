# MOFIT Demo

## Overview

Simple little demo for the Seminar 490 class. The Python script uses pose estimation to analyze and provide feedback on hammer curls. It detects keypoints on the human body and calculates angle of elbow to determine of each curl. It also counts the number of reps.

## Dependencies

- Python 3.x
- OpenCV
- YOLOv8

Use the requirements.txt to install the needed libraries

```bash
pip install -r requirements.txt
```

In addition, you will need to download the Yolov8 Medium Pose Detection Model and put it in the same directory. You can download the offical model from Ultralytics [here](https://github.com/ultralytics/assets/releases/download/v8.1.0/yolov8m-pose.pt)

## Running the Script

Execute the following

```python
python script.py
```

## Note

If you run this script inside VSCode, VSCode may prompt you to allow permission to access your webcam. If you would rather analyze a pre-recorded video than live streaming from your webcam, simply change the `VideoCapture(0)` source from 0 to the name of your video file.
