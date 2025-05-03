import cv2  # OpenCV for video processing
import os   # For file and directory operations
import tkinter as tk  # For file selection dialog
from tkinter import filedialog

def video_to_images(output_folder, maxframes):
    """
    Extracts frames from a video and saves them as images.

    Args:
        output_folder (str): Path to the folder where images will be saved.
        maxframes (int): Maximum number of frames to extract.
    """

    # Initialize and hide the tkinter root window (used for file dialog)
    root = tk.Tk()
    root.withdraw()

    # Prompt the user to select a video file
    video_path = filedialog.askopenfilename(title="Select a file for the process.")

    if video_path:
        print(f"File selected: {video_path}")
    else:
        print("No file selected.")
        return  # Exit if no file is chosen

    # Create the output folder if it doesn't exist
    if not os.path.exists(output_folder):
        os.makedirs(output_folder)

    # Open the video file
    video_capture = cv2.VideoCapture(video_path)
    frame_count = 0

    # Read frames one by one
    while True:
        success, frame = video_capture.read()  # Read a frame from the video

        if not success:
            break  # Exit loop when no more frames are available or read fails

        # Stop extracting if the maximum number of frames is reached
        if maxframes is not None and frame_count >= maxframes:
            break

        # Construct image file name and save frame as JPG
        image_name = f"frame_{frame_count:04d}.jpg"
        image_path = os.path.join(output_folder, image_name)
        cv2.imwrite(image_path, frame)

        frame_count += 1

    # Release the video file
    video_capture.release()
    print(f"Extracted {frame_count} frames to {output_folder}")

# This block runs only if the script is executed directly (not imported)
if __name__ == "__main__":
    output_folder = "Packages/HybridRelightable3DGaussianRendering/Runtime/ML/output_images"  # Output path for extracted images
    maxframes = 300  # Limit the number of frames to extract
    video_to_images(output_folder, maxframes)