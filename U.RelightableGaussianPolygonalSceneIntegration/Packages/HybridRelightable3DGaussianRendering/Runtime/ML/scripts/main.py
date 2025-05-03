import os
from train import Trainer
ply_file = os.path.join("Packages", "com.isu_sdmay25_40.hybrid_relightable_3d_gaussian_rendering", "Runtime", "ML", "sfm", "output.ply")
colmap_model = os.path.join("Packages", "com.isu_sdmay25_40.hybrid_relightable_3d_gaussian_rendering", "Runtime", "ML", "sfm", "sparse", "0")
image_folder = os.path.join("Packages", "com.isu_sdmay25_40.hybrid_relightable_3d_gaussian_rendering", "Runtime", "ML", "output_images")

print("---------------Optimizing Model-----------------------")

trainer = Trainer(
    colmap_model_dir = colmap_model,
    image_dir        = image_folder,
    ply_path         = ply_file
)

trainer.train()



trainer.gaussians.save_ply("Packages/com.isu_sdmay25_40.hybrid_relightable_3d_gaussian_rendering/Runtime/ML/optimizedPLY.ply")