import torch
import math
torch.cuda.is_available()
from diff_gaussian_rasterization import GaussianRasterizationSettings, GaussianRasterizer

class Renderer:
    def __init__(self, debug: bool = False, compute_cov3D: bool = False):
        self.compute_cov3D = compute_cov3D
        self.debug = debug

    def render(self, camera, pc, bg_color: torch.Tensor, scaling_modifier: float = 1.0):
        screenspace = torch.zeros_like(pc.get_xyz, requires_grad=True, device="cuda")
        try: screenspace.retain_grad()
        except: pass

        tanfovx = math.tan(camera.FoVx * 0.5)
        tanfovy = math.tan(camera.FoVy * 0.5)

        settings = GaussianRasterizationSettings(
            image_height = int(camera.image_height),
            image_width = int(camera.image_width),
            tanfovx = tanfovx,
            tanfovy = tanfovy,
            bg = bg_color,
            scale_modifier = scaling_modifier,
            viewmatrix = camera.world_view_transform,
            projmatrix = camera.full_proj_transform,
            sh_degree = pc.active_sh_degree,
            campos = camera.camera_center,
            prefiltered = False,
            debug = self.debug
        )
        rasterizer = GaussianRasterizer(raster_settings=settings)

        means3D = pc.get_xyz 
        means2D = screenspace
        opacities = pc.get_opacity
        scales = pc.get_scaling
        rotations = pc.get_rotation
        cov3D = None
        colors_precomp = pc.get_features[:,:,0]
        shs = None
        
        outs = rasterizer(
            means3D = means3D,
            means2D = means2D,
            shs = shs,
            colors_precomp = colors_precomp,
            opacities = opacities,
            scales = scales,
            rotations = rotations,
            cov3D_precomp = cov3D
        ) 
        
        if len(outs) == 3:
            rendered, radii, depth = outs
        else:
            rendered, radii = outs
            depth = None
        
        return {
            "render": rendered.clamp(0.0, 1.0),
            "viewspace_points": screenspace,
            "visibility_filter": (radii > 0).nonzero(),
            "radii": radii,
            "depth": depth
        }