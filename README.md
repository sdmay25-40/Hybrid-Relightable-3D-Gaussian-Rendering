

# Hybrid Relightable 3D Gaussian Rendering  
Team 40 – Senior Design May 2025  
Jackson Vanderheyden (Graphics Scope Manager), Brian Xicon (Machine Learning Scope Manager), Luke Broglio (Schedule Manager), Ethan Gasner (Documentation Manager), Kyle (Communication Manager), <br>  
| Project Page [https://sdmay25-40.sd.ece.iastate.edu/](#) | [Demo Video (coming soon)](#) | [Final Report (coming soon)](#) | [Unity Plugin (WIP)](#) |<br>  
![Teaser](assets/teaser.png)


This repository contains the official implementation of our senior design project **Hybrid Relightable 3D Gaussian Rendering**, which enables high-quality 3D models to be reconstructed from smartphone video using AI. Our system outputs relightable Gaussian-based 3D scenes that can be used in Unity for real-time applications or exported for 3D printing.


<a href="https://git.ece.iastate.edu/sd/sdmay25-40/"><img height="100" src="assets/logo_isu_ece.png"></a>
<a href="https://www.iastate.edu/"><img height="100" src="assets/logo_isu.png"></a>


---


## Abstract  
*We present a novel hybrid pipeline for generating relightable 3D Gaussian splats from consumer-grade smartphone video. Our system combines classical Structure-from-Motion with neural Gaussian optimization in PyTorch. The output is a scene of 3D Gaussians that can be rendered in real time in Unity with custom lighting setups or exported for 3D printing. This enables realistic asset creation from everyday footage for use in games, film, AR/VR, and rapid prototyping.*


---
##Prerequisites before use: 
#Software Requirements:
    Must have Unity installed on your computer.
#Hardware Requirements:
    Must have an Nvidia's graphics card in your computer.




## Installation




### 1. Install Python
Follow your OS instructions or [download Python 3.10+ from the official site](https://www.python.org/downloads/).  
Add Python to PATH during installation.


### 2. Clone the Repository
```bash
git clone https://github.com/your-org/hybrid-gaussian-rendering.git
cd hybrid-gaussian-rendering
```


### 3. Create and Activate a Virtual Environment
```bash
python -m venv env
source env/bin/activate      # On Linux/Mac
env\Scripts\activate         # On Windows
```


### 4. Install Requirements
```bash
pip install -r requirements.txt
```


---


## Pipeline Overview


```mermaid
graph TD
A[Smartphone Video] --> B[COLMAP SfM + MVS]
B --> C[Initial Point Cloud]
C --> D[Gaussian Scene Optimization (PyTorch)]
D --> E1[Unity Ray Tracer]
D --> E2[3D Printing Export (Optional)]
```


---


## Features


- 📸 Convert smartphone video into a full 3D scene
- 💡 Relightable Gaussian-based scene rendering
- ⚡ Unity-compatible real-time ray tracer
- 🖨️ Optional STL/OBJ export for 3D printing
- 🔁 Seamless Structure-from-Motion integration


---
## File Structure
//Picture of our file Structure

## Usage
#1) Take the video files you desire to turn into a 3D model.
#2) Drag them to the BLANK folder in the unity project folder.
#3) Hit play in Unity, this may take a while depending on the length of your video. The generated 3D model should be outputted to the BLANK folder. 
ANY Other instructions for Running our project.





## BibTeX


```bibtex
@misc{team40_gaussian_2025,
  title={Hybrid Relightable 3D Gaussian Rendering},
  author={Senior Design Team 40},
  year={2025},
  note={Iowa State University Senior Design Project}
}
```


---


## Acknowledgements


Special thanks to Dr. Mitra for guidance, the ECE Department at Iowa State University, and the authors of [3D Gaussian Splatting (Kerbl et al.)](https://repo-sam.inria.fr/fungraph/3d-gaussian-splatting/) for inspiration.


---


## License


This project is released under the MIT License. See [LICENSE](LICENSE) for details.
