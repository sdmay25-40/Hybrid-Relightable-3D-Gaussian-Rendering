import subprocess  # To run external processes like COLMAP
import os          # For file and path management
import tkinter as tk  # For file dialog GUI
from tkinter import filedialog
from pathlib import Path  # For cleaner path handling

# Function to check if GPU is available for COLMAP (avoids crash if CUDA is unavailable)
def is_gpu_available(batchFile):
    try:
        result = subprocess.run(
            [batchFile, "feature_extractor", "--SiftExtraction.use_gpu", "1", "--help"],
            capture_output=True, text=True, shell=True, timeout=10
        )
        if "not compiled with CUDA" in result.stderr or "No OpenGL context" in result.stderr:
            return False
        return True
    except Exception as e:
        print(f"[WARN] GPU check failed: {e}")
        return False

# Function to run COLMAP with GPU, and fallback to CPU if "Not enough GPU memory" error occurs
def run_colmap_with_gpu_fallback(cmd, use_gpu_flag_name, use_gpu):
    attempt_cmd = cmd + [use_gpu_flag_name, str(int(use_gpu))]

    result = subprocess.run(attempt_cmd, capture_output=True, text=True, shell=True, timeout=3600)

    # Check for GPU memory error and retry with CPU
    if use_gpu and "Not enough GPU memory" in result.stderr:
        print("[WARN] GPU memory insufficient. Retrying with CPU...")
        attempt_cmd = cmd + [use_gpu_flag_name, "0"]
        result = subprocess.run(attempt_cmd, capture_output=True, text=True, shell=True, timeout=3600)

    return result


# Print the current working directory to verify the script is running in the right place
print("CWD: ", os.getcwd())

# Initialize and hide the root tkinter window (for file selection dialog)
root = tk.Tk()
root.withdraw()

# Define paths used throughout the script (could be dynamically set later)
datasetPath = Path("Packages/HybridRelightable3DGaussianRendering/Runtime/ML/sfm")
imagePath = Path("Packages/HybridRelightable3DGaussianRendering/Runtime/ML/output_images")
databasePath = datasetPath / 'database.db'

if databasePath.exists():
    try:
        databasePath.unlink()
        print(f"[CLEANUP] Removed old COLMAP database: {databasePath}")
    except Exception as e:
        print(f"[WARN] Could not delete old database: {e}")

# Prompt the user to select the COLMAP batch executable
batchFile = Path("Packages/HybridRelightable3DGaussianRendering/Runtime/ML/colmap/COLMAP.bat")

for meta_file in imagePath.glob("*.jpg.meta"):
    try:
        meta_file.unlink()
        print(f"[CLEANUP] Removed meta file: {meta_file.name}")
    except Exception as e:
        print(f"[WARN] Could not delete {meta_file}: {e}")

# Check for GPU support and adjust script execution accordingly
use_gpu = is_gpu_available(batchFile)
print(f"[INFO] GPU detected: {use_gpu}")

# ---------- Structure-from-Motion Pipeline Begins ----------

print("[INFO] Starting SfM Script utilizing Colmap:")

# --- Step 1: Feature Extraction ---
print("[INFO] Starting feature_extractor:")
base_cmd = [
    str(batchFile),
    "feature_extractor",
    "--database_path", str(databasePath),
    "--image_path", str(imagePath)
]
result = run_colmap_with_gpu_fallback(base_cmd, "--SiftExtraction.use_gpu", use_gpu)
print(result.stdout, result.stderr)
print("[INFO] Feature extraction finished.")

# --- Step 2: Sequential Feature Matching ---
print("[INFO] Starting Sequential Matching: (May Take A While)")
base_cmd = [
    batchFile,
    "sequential_matcher",
    "--SiftMatching.max_num_matches", "32768",
    "--database_path", str(databasePath)
]
result = run_colmap_with_gpu_fallback(base_cmd, "--SiftMatching.use_gpu", use_gpu)
print(result.stdout, result.stderr)
print("[INFO] Sequential Matcher finished.")

# --- Step 3: Create sparse directory for storing sparse reconstruction ---
sparsePath = datasetPath / 'sparse'
os.makedirs(sparsePath, exist_ok=True)
print("[INFO] Sparse directory created.")
print(result.stdout, result.stderr)

# --- Step 4: Run the COLMAP Mapper to generate sparse reconstruction ---
print("[INFO] Starting Mapper")
result = subprocess.run([
    batchFile,
    "mapper",
    "--database_path", databasePath,
    "--image_path", imagePath,
    "--output_path", sparsePath
], capture_output=True, text=True, timeout=3600)
print(result.stdout, result.stderr)
print("[INFO] Sparse Mapper finished.")

print("[INFO] Converting binary COLMAP model to TXT")
bin_model_dir = sparsePath / '0'
result = subprocess.run([
    str(batchFile),
    "model_converter",
    "--input_path",  str(bin_model_dir),
    "--output_path", str(bin_model_dir),
    "--output_type", "TXT"
], capture_output=True, text=True, shell=True, timeout=3600)
print(result.stdout, result.stderr)
print("[INFO] Binary - TXT conversion finished.")

# --- Step 5: Convert model to PLY format for visualization/export ---
print("[INFO] Starting Model Converter")
sparseDataPath = sparsePath / '0'  # Default subdirectory where COLMAP stores the first model
outputPath = datasetPath / 'output.ply'  # Final 3D model output

print(sparseDataPath)
result = subprocess.run([
    str(batchFile),
    "model_converter",
    "--input_path", str(sparseDataPath),
    "--output_path", str(outputPath),
    "--output_type", "PLY"
], capture_output=True, text=True, shell=True, timeout=3600)
print(result.stdout, result.stderr)
print("[INFO] Model conversion finished.")
