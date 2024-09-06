from ultralytics import YOLO
import numpy as np
import cv2


def calculate_angle(a, b, c):
    a = np.array(a)  # Shoulder
    b = np.array(b)  # Elbow
    c = np.array(c)  # Wrist
    radians = np.arctan2(c[1] - b[1], c[0] - b[0]) - np.arctan2(
        a[1] - b[1], a[0] - b[0]
    )
    angle = np.abs(radians * 180.0 / np.pi)
    if angle > 180.0:
        angle = 360 - angle
    return angle


def draw_keypoints(frame, keypoints):
    colors = [
        (0, 255, 0),
        (0, 0, 255),
        (255, 0, 0),
    ]  # Colors for shoulder, elbow, wrist
    labels = ["Shoulder", "Elbow", "Wrist"]
    for idx, (point, color) in enumerate(zip(keypoints, colors)):
        x, y = int(point[0]), int(point[1])
        cv2.circle(frame, (x, y), 5, color, -1)  # Draw the keypoint
        cv2.putText(
            frame, labels[idx], (x + 10, y), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 1
        )


model = YOLO("yolov8m-pose.pt")
cap = cv2.VideoCapture(0)

rep_count = 0
in_rep = False

while True:
    ret, frame = cap.read()
    if not ret:
        break

    results = model(frame)

    if results and results[0].keypoints.xy.shape[1] >= 10:
        keypoints = results[0].keypoints.xy[0].numpy()

        # Note, this only corresponds to persons left shoulder, left elbow, left wrist
        shoulder = keypoints[5]
        elbow = keypoints[7]
        wrist = keypoints[9]

        angle = calculate_angle(shoulder, elbow, wrist)
        draw_keypoints(frame, [shoulder, elbow, wrist])
        feedback = "Good" if 75 <= angle <= 100 else "Needs Improvement"

        # Determine if the arm is in a curl position
        if angle <= 100:
            if not in_rep:
                in_rep = True
        elif angle >= 160 and in_rep:
            # Complete one full extension from the curl, count as one rep
            rep_count += 1
            in_rep = False  # Reset for the next rep

        cv2.rectangle(frame, (5, 10), (350, 120), (250, 250, 250), -1)

        cv2.putText(
            frame,
            f"Elbow Angle: {angle:.2f} degrees",
            (10, 30),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (217, 111, 111),
            2,
        )
        cv2.putText(
            frame,
            f"Feedback: {feedback}",
            (10, 60),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (217, 111, 111),
            2,
        )
        cv2.putText(
            frame,
            f"Reps: {rep_count}",
            (10, 90),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (217, 111, 111),
            2,
        )
    else:
        cv2.putText(
            frame,
            "Insufficient keypoints",
            (10, 30),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.7,
            (0, 0, 255),
            2,
        )

    cv2.imshow("Bicep Curl Analysis", frame)
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cap.release()
cv2.destroyAllWindows()
