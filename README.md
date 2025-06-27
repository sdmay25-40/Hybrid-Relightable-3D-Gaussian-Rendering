# Hybrid Relightable 3D Gaussian Rendering README  
Team 40 – Senior Design May 2025  
Jackson Vanderheyden (Graphics Scope Manager), Brian Xicon (Machine Learning Scope Manager), Luke Broglio (Schedule Manager), Ethan Gasner (Documentation Manager), Kyle Kohl (Communication Manager), <br>  
| [Project Page](https://sdmay25-40.sd.ece.iastate.edu/)|<br>  
![Teaser](images/guitar.png)

This repository contains the official implementation of our senior design project **Hybrid Relightable 3D Gaussian Rendering**, which enables high-quality 3D models to be reconstructed from video using AI. Our system converts video into realistic, relightable 3D Gaussian models that can be seamlessly used within the Unity Game Engine. Users can place and transform these models alongside Unity-supported objects, enabling intuitive scene authoring.


---


## Abstract  
*We present a novel hybrid pipeline for generating relightable 3D Gaussian splats from video. Built on Unity Editor Version 2022.3.50f1, our system combines classical Structure-from-Motion with gaussian optimization using a neural network in PyTorch. The output is a scene of 3D Gaussians that can be rendered in real time in Unity with custom lighting setups. This enables realistic asset creation from everyday footage for use in games, film, AR/VR, and rapid prototyping.*


---


## Prerequisites before use: 
### Software Requirements:
- Must have Python 3.10+
- Must have the plyfile libary installed 
- Must have the ipython libary installed 
- Must have the torch libary installed 
- Must have opencv-python libary installed
- Must have torchvision libary installed
- Must have tkinter libary installed. (_tkinter must be installed globally_)
- Must have Unity installed on your computer.
- Must have CUDA installed on your computer.
- Musrt have Git installed on your computer.

### Hardware Requirements:
- Must have an NVIDIA graphics card capable of running CUDA for optimizing 3D Gaussians.


## Installation
### 1. Install Python
Follow these [instructions](https://phoenixnap.com/kb/how-to-install-python-3-windows) for Windows.  

Follow these [instructions](https://phoenixnap.com/kb/install-python-mac)  for Mac.  

1. Download [Python Executable Installer](https://www.python.org/downloads/)
1. Run Executable Installer
1. Add Python to PATH during installation
1. Verify Python is installed on Windows
1. Verify pip is installed on Windows

### 2. Install tkinter
#### For Windows
Run this command in your terminal 
```
pip install tkinter
```
#### For Linux
```
sudo apt-get install python3-tk
```

### 3. Install other dependencies
Run this command in your terminal:
```
pip install plyfile ipython torch torchvision opencv-python
```

---



## Pipeline Overview

```mermaid
graph TD
    A["Video"] --> B["COLMAP SfM + MVS"]
    B --> C["Initial Point Cloud"]
    C --> D["Gaussian Scene Optimization (PyTorch)"]
    D --> E["Unity Hybrid Renderer"]

```

---


## Features

-  Seamless Structure-from-Motion integration
-  Convert video into a fully modeled 3D scene
-  Hybrid Triangle-Gaussian-based scene rendering
-  Unity-compatible real-time ray tracer


---


## File Structure
```
Assets/
└── Hybrid Relightable 3D Gaussian Rendering/        
    ├── Scripts/
    ├── Prefabs/
    ├── Materials/
    ├── Textures/
    ├── Scenes/
    └── Documentation/
```


## Usage

### Setup Project
1. Start a new Unity project
1. Open the **Package Mangager** 
    * It can be found within the **Window** tab.
    ![Package Manager](images/PackageManager.png)

1. Add the package from git 
    1. Click on the '+' symbol towards the top of the window 
    2. Click Add Package from git URL
    3. Enter the URL https://github.com/sdmay25-40/Hybrid-Relightable-3D-Gaussian-Rendering.git. 
![Add from Git](images/gitAdd.png)
1. The project should now be able to be found in the Packages folder 
of the project explorer.
![Project Explorer View](images/projectExplorer.png)
2. Import the 3D Model Generation Scene from the Samples tab of the package.
![Samples tab](images/samples.png)

### Build Gaussian Model with machine learning 
1. Open the 3D Model Generation Scene from the Project Explorer
1. In the python script runner object set the Python script runner attribute 
to be the path to your python executable.
![Python attribute](images/pyAttribute.png)
1. Run the unity scene
1. When prompted, select the video files you desire to turn into a 3D model.
1. The progress of the render is outputed to the Unity Console. 

### Renderer Setup
1. Add the Main Camera prefab from the Runtime/Prefabs folder of the 
Hybrid Relightable 3D Gaussian package to your scene.
    * Remove any other cameras.
2. For every Gaussian model you would like to render add a GaussianModel 
prefab from the Runtime/Prefabs folder.
    * Then drag the .ply model to the Gaussian File attribute.
    * You might need to adjust the Gaussian Space Scale attribute based on the 
    gaussians look in the scene.
    ![Gaussian Attributes](images/gaussAttributes.png)
3. Add any polygon models you want to render to the scene.
4. Run the scene by presseing the play button.

## BibTeX

```bibtex
@misc{team40_gaussian_2025,
  title={Hybrid Relightable 3D Gaussian Rendering},
  author={Senior Design Team 40, Jackson Vanderheyden, Brian Xicon, Luke Broglio, Ethan Gasner, Kyle Kohl},
  year={2025},
  note={Iowa State University Senior Design Project}
}
```
---


## Acknowledgements

Special thanks to Dr. Mitra for guidance, the ECE Department at Iowa State University, and the authors of [3D Gaussian Splatting for Real-Time Radiance Field Rendering (Kerbl et al.)](https://repo-sam.inria.fr/fungraph/3d-gaussian-splatting/), [Relightable 3D Gaussian](https://nju-3dv.github.io/projects/Relightable3DGaussian/), and [3D Gaussian Ray Tracing of Particle Scenes](https://gaussiantracer.github.io/) for inspiration.

<a href="https://www.ece.iastate.edu/"><img height="200" src="images/ISUECE.jpg" style="margin-right: 100px;"></a>
<a href="https://www.iastate.edu/"><img height="200" src="images/ISULogo.png"></a>
---
