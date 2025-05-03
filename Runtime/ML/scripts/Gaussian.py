import torch
import numpy as np
from torch import nn
from plyfile import PlyData, PlyElement
from utils.general_utils import inverse_sigmoid, build_scaling_rotation, strip_symmetric
from utils.sh_utils import RGB2SH
from utils.graphics_utils import BasicPointCloud
from utils.system_utils import mkdir_p
from simple_knn._C import distCUDA2
import os

class GaussianModel(nn.Module):
    def __init__(self,sh_degree):
        super().__init__()
        self.max_sh_degree = sh_degree
        self.active_sh_degree = 0
        self._xyz = None
        self._features = None
        self._scaling = None
        self._rotation = None
        self._opacity = None
        self.scaling_activation = torch.exp
        self.opacity_activation = torch.sigmoid
        self.inverse_opacity_activation = inverse_sigmoid
        self.rotation_activation = lambda r: nn.functional.normalize(r, dim=1)

    @property
    def get_xyz(self):
        return self._xyz

    @property
    def get_rotation(self):
        return self.rotation_activation(self._rotation)
    
    @property
    def get_features(self):
        return self._features

    @property
    def get_scaling(self):
        return self.scaling_activation(self._scaling)

    @property
    def get_opacity(self):
        return self.opacity_activation(self._opacity)

    def oneupSHdegree(self):
        if self.active_sh_degree < self.max_sh_degree:
            self.active_sh_degree += 1

    #Function to get gaussian point cloud from normal point cloud data.
    def create_from_pcd(self, pcd: BasicPointCloud, spatial_lr_scale: float = 1.0):
        #Converts to tensors for the GPU
        pts = torch.tensor(np.asarray(pcd.points)).float().cuda()
        cols = torch.tensor(np.asarray(pcd.colors)).float().cuda()
        #Computes the SH coefficients from the RGB values
        sh = RGB2SH(cols)

        
        F = 3 #Each gaussian has 3 features (RGB channels)
        C = (self.max_sh_degree+1)**2 #Total coeffcieints for feature for SH
        feats = torch.zeros((pts.shape[0], F, C), device="cuda") #Feature tensor that stores each gaussian and their channels and coefficients.
        feats[:, :3, 0] = sh

        #Initializes the scaling of distance between the points
        dist2 = torch.clamp_min(distCUDA2(pts), 1e-7)
        scales = torch.log(torch.sqrt(dist2)).unsqueeze(1).repeat(1, 3)

        rots = torch.zeros((pts.shape[0],4), device="cuda")
        rots[:,0] = 1
        
        #Initializes the opacity
        ops = self.inverse_opacity_activation(0.1 * torch.ones((pts.shape[0], 1), device="cuda"))

        self._xyz = nn.Parameter(pts.requires_grad_(True))
        self._features = nn.Parameter(feats.requires_grad_(True))
        self._scaling = nn.Parameter(scales.requires_grad_(True))
        self._rotation = nn.Parameter(rots.requires_grad_(True))
        self._opacity = nn.Parameter(ops.requires_grad_(True))

    #Adam optimizer for all of the different parameters
    def training_setup(self, lr_dict):
        params = [
            {'params': [self._xyz], 'lr': lr_dict['xyz']},
            {'params': [self._features], 'lr': lr_dict['feat']},
            {'params': [self._scaling], 'lr': lr_dict['scale']},
            {'params': [self._rotation], 'lr': lr_dict['rot']},
            {'params': [self._opacity], 'lr': lr_dict['opacity']}
        ]
        self.optimizer = torch.optim.Adam(params, lr=0.0, eps=1e-15)

    def update_learning_rate(self, new_lr):
        for g in self.optimizer.param_groups:
            g['lr'] = new_lr
    
    def save_ply(self, path):
        mkdir_p(os.path.dirname(path))
        xyz = self._xyz.detach().cpu().numpy()
        P = xyz.shape[0]
        feats = self._features.detach().cpu().numpy().reshape(P, -1) 
        scales = self.get_scaling.detach().cpu().numpy()  
        ops = self.get_opacity.detach().cpu().numpy()
        dtype = []
        dtype += [('x','f4'),('y','f4'),('z','f4')]
        for i in range(feats.shape[1]):
            dtype.append((f'f{i}','f4'))
        dtype += [('scale_x','f4'),('scale_y','f4'),('scale_z','f4')]
        dtype += [('opacity','f4')]

        data = np.hstack((xyz, feats, scales, ops))
        elements = np.array([tuple(r) for r in data], dtype=dtype)
        el = PlyElement.describe(elements, 'vertex')
        PlyData([el]).write(path)

    def load_ply(self, path):
        plydata = PlyData.read(path)
        arr = plydata['vertex'].data
        names = arr.dtype.names
        xyz = np.vstack((arr['x'], arr['y'], arr['z'])).T
        feat_fields = [n for n in names if n.startswith('f')]
        feat_fields.sort(key=lambda s: int(s[1:]))
        feats_flat = np.vstack([arr[f] for f in feat_fields]).T  # (P, 3·C)
        C = (self.max_sh_degree + 1)**2
        feats = feats_flat.reshape(-1, 3, C)
        scales = np.vstack((arr['scale_x'], arr['scale_y'], arr['scale_z'])).T
        ops = arr['opacity'][..., None]
        self._scaling  = nn.Parameter(torch.log(torch.from_numpy(scales.astype(np.float32)).to(device)))
        self._opacity  = nn.Parameter(torch.logit(torch.from_numpy(ops.astype(np.float32)).to(device), eps=1e-6))



