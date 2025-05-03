import os, random, torch, numpy as np
import torch.nn.functional as F
import torch.nn as nn
from torch.optim import Adam
from torchvision.utils import make_grid
from torchvision.transforms.functional import to_pil_image
from IPython.display import display
from Camera import CameraDataset
from Gaussian import GaussianModel
from Renderer import Renderer
from utils.graphics_utils import BasicPointCloud
from utils.general_utils import get_expon_lr_func
from utils.loss_utils import l1_loss, ssim
from PIL import Image

class Trainer:
    def __init__(self, colmap_model_dir: str, image_dir: str, ply_path: str, sh_degree: int = 2, device: str = "cuda",
        num_steps: int = 20000, pos_lr_init: float = 1e-2, pos_lr_final: float = 1e-4, feat_lr: float = 5e-3,
        scale_lr: float = 5e-3, rot_lr: float = 5e-3, opacity_lr: float = 1e-3, lambda_dssim: float = 0.1,
        random_bg: bool = True, sh_degree_step: int = 500, prune_every: int = 500, opacity_thresh: float = 0.02):
        
        self.device = device
        self.num_steps = num_steps
        self.lambda_dssim = lambda_dssim
        self.random_bg = random_bg
        self.sh_degree_step = sh_degree_step
        self.prune_every = prune_every
        self.opacity_thresh = opacity_thresh

        #Loads cameras from COLMAP
        self.cams    = CameraDataset(colmap_model_dir, image_dir, device=device)
        self.cam_ids = list(self.cams.cameras.keys())

        #Reads given ply file and converts it to a basic point cloud.
        from plyfile import PlyData
        ply = PlyData.read(ply_path).elements[0].data
        pts = np.stack([ply['x'], ply['y'], ply['z']], axis=1)
        cols = np.stack([ply['red'], ply['green'], ply['blue']], axis=1) / 255.0
        pcd = BasicPointCloud(pts, cols, np.zeros_like(pts))

        #Initalizes our gaussian model and renderer
        self.gaussians = GaussianModel(sh_degree).to(device)
        self.gaussians.create_from_pcd(pcd, spatial_lr_scale=1.0)
        self.renderer  = Renderer(debug=False, compute_cov3D=False)

        #Sets up the different learning rates of our parameters
        self.lr_dict = {
            'xyz'    : pos_lr_init,
            'feat'   : feat_lr,
            'scale'  : scale_lr,
            'rot'    : rot_lr,
            'opacity': opacity_lr
        }
        self.gaussians.training_setup(self.lr_dict)

        #Scheduler that changes the learning rate for position throughout the training
        self.xyz_scheduler = get_expon_lr_func(pos_lr_init, pos_lr_final, max_steps=num_steps)

        #Sets background color of renderer to be white
        self.bg = torch.tensor([1.0,1.0,1.0], device=device)

    def train(self):
        for i in range(1, self.num_steps+1):
            #Updates the positon learning rate
            new_lr = self.xyz_scheduler(i)
            self.gaussians.update_learning_rate(new_lr)

            #Increases the SH degree after a designated amount of steps to increase sharpness
            if i % self.sh_degree_step == 0:
                self.gaussians.oneupSHdegree()

            #Grabs random camera to train with in each iteration
            cam_id = random.choice(self.cam_ids)
            cam = self.cams[cam_id]

            #Change the background color randomly if random_bg is set to True
            bg = torch.rand(3, device=self.device) if self.random_bg else self.bg

            #Renders the gaussian point cloud with the given camera
            out  = self.renderer.render(
                camera = cam,
                pc = self.gaussians,
                bg_color = bg,
                scaling_modifier = 1.0
            )
            pred = out["render"]

            #Ground truth image
            gt = cam.original_image.to(pred.device)

            #Loss value to calculate gaussian render vs ground truth image loss
            l1_val   = l1_loss(pred, gt)
            ssim_val = ssim(pred.unsqueeze(0), gt.unsqueeze(0))
            loss     = (1 - self.lambda_dssim)*l1_val + self.lambda_dssim*(1 - ssim_val)

            #Backward pass and optimizer step
            self.gaussians.optimizer.zero_grad(set_to_none=True)
            loss.backward()
            self.gaussians.optimizer.step()

            #Prune any gaussians with low opacity
            if i % self.prune_every == 0:
                with torch.no_grad():
                    op    = self.gaussians.get_opacity.view(-1)
                    keep  = (op > self.opacity_thresh)
                    if keep.sum() < keep.numel():
                        for name in ('_xyz','_features','_scaling','_rotation','_opacity'):
                            param = getattr(self.gaussians, name)
                            new_p = nn.Parameter(param.data[keep].clone(), requires_grad=True)
                            setattr(self.gaussians, name, new_p)
                        self.gaussians.training_setup(self.lr_dict)
                        self.gaussians.update_learning_rate(new_lr)

            #Logs for development
            if i % 100 == 0:
                with torch.no_grad():
                    mse = F.mse_loss(pred, gt)
                    ps  = -10.0 * torch.log10(mse)
                print(f"[Step {i:5d}]  L1={l1_val:.4e}  SSIM={ssim_val:.4f}  PSNR={ps:.2f}  lr={new_lr:.1e}")
                #grid = self.show_comparison(cam_id)
                #grid.show(title=f"Step {i}")

        print("Training complete.")

    #Renders and displays the point cloud output vs truth image.
    def show_comparison(self, cam_id):
        cam  = self.cams[cam_id]
        out  = self.renderer.render(cam, self.gaussians, self.bg, 1.0)
        rend = out["render"].cpu()
        gt   = cam.original_image.cpu()
        from torchvision.utils import make_grid
        from torchvision.transforms.functional import to_pil_image
        grid = make_grid([gt, rend], nrow=2, padding=10)
        return to_pil_image(grid)