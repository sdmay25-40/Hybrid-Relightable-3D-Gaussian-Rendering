import os
import numpy as np
import torch
from PIL import Image
from utils.colmap_loader import read_intrinsics_text, read_extrinsics_text, qvec2rotmat
from utils.graphics_utils import getWorld2View2, getProjectionMatrix
from utils.general_utils import PILtoTorch

class Camera:
    def __init__(self, cam_model, img_entry, image_dir, device="cuda"):
        #Intrinsic values of the camera
        w, h = cam_model.width, cam_model.height
        params = cam_model.params

        #Selecting the correct camera model
        if cam_model.model == "PINHOLE":
            fx, fy, cx, cy = params
        elif cam_model.model == "SIMPLE_RADIAL":
            f, cx, cy, _ = params
            fx = fy = f
        else:
            raise NotImplementedError(cam_model.model)

        #Computes the angles for field of view
        self.FoVx = 2 * np.arctan(w  / (2 * fx))
        self.FoVy = 2 * np.arctan(h  / (2 * fy))

        #Extrinsic camera values
        R = qvec2rotmat(img_entry.qvec)
        T = img_entry.tvec

        #Converts the world to view matrix into a tensor on our GPU (Converts world space to camera space)
        W2V_np = getWorld2View2(R, T)
        W2V = torch.from_numpy(W2V_np).transpose(0,1).to(device)

        #Project matrix for camera
        P_mat = getProjectionMatrix(znear=0.01, zfar=100.0,fovX=self.FoVx, fovY=self.FoVy)

        #If statement to see if its a tensor or not and act accordingly
        if isinstance(P_mat, torch.Tensor):
            P = P_mat.transpose(0,1).to(device)
        else:
            P = torch.from_numpy(P_mat).transpose(0,1).to(device)

        #Gets the full projection of the camera
        self.world_view_transform = W2V
        self.full_proj_transform  = (W2V.unsqueeze(0).bmm(P.unsqueeze(0))).squeeze(0)

        #Grab the cameras center
        inv = torch.inverse(W2V)
        self.camera_center = inv[3, :3]

        #Loads the truth image for each camera
        img_path = os.path.join(image_dir, img_entry.name)
        pil = Image.open(img_path).convert("RGB")
        t = PILtoTorch(pil, (w, h)).clamp(0,1).to(device)
        self.original_image = t[:3]

        #Stores the dimensions of the camera
        self.image_width = w
        self.image_height = h

class CameraDataset:
    def __init__(self, model_dir, image_dir, device="cuda"):
        cams_txt = os.path.join(model_dir, "cameras.txt")
        imgs_txt = os.path.join(model_dir, "images.txt")
        intrin = read_intrinsics_text(cams_txt)
        extrin = read_extrinsics_text(imgs_txt)

        self.cameras = {}
        #Creates the list of cameras given by the cameras.txt and images.txt files from COLMAP.
        for img_id, img_entry in extrin.items():
            cam_model = intrin[img_entry.camera_id]
            cam = Camera(cam_model, img_entry, image_dir, device=device)
            self.cameras[img_id] = cam

    def __len__(self):
        return len(self.cameras)

    def __getitem__(self, img_id):
        return self.cameras[img_id]


        