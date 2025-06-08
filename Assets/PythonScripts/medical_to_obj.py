import os
import numpy as np
import nibabel as nib
import pydicom
from skimage import measure
import trimesh

def load_nifti(path):
    img = nib.load(path)
    data = img.get_fdata()
    return np.transpose(data, (2, 1, 0))  # Z, Y, X

def load_dicom_series(folder_path):
    slices = []
    for f in os.listdir(folder_path):
        if f.endswith(".dcm"):
            slices.append(pydicom.dcmread(os.path.join(folder_path, f)))
    slices.sort(key=lambda x: float(x.ImagePositionPatient[2]))
    volume = np.stack([s.pixel_array for s in slices])
    return volume

def volume_to_mesh(volume, threshold=300):
    verts, faces, _, _ = measure.marching_cubes(volume, level=threshold)
    return verts, faces

def save_obj(filename, verts, faces):
    mesh = trimesh.Trimesh(vertices=verts, faces=faces)
    mesh.export(filename)
    print(f"Exported: {filename}")

def convert_medical_to_obj(input_path, output_obj_path, is_dicom=False):
    if is_dicom:
        volume = load_dicom_series(input_path)
    else:
        volume = load_nifti(input_path)

    verts, faces = volume_to_mesh(volume)
    save_obj(output_obj_path, verts, faces)


if __name__ == "__main__":
    import sys

    if len(sys.argv) < 4:
        print("Uso: python medical_to_obj.py <input_path> <output_path> <tipo>")
        sys.exit(1)

    input_path = sys.argv[1]
    output_obj = sys.argv[2]
    tipo = sys.argv[3].lower()
    is_dicom = tipo == "dicom"

    print(f"Convirtiendo: {input_path} -> {output_obj} (DICOM: {is_dicom})")
    convert_medical_to_obj(input_path, output_obj, is_dicom)

