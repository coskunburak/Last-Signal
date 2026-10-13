#!/usr/bin/env python3
"""Local authoring only: copy selected vendor maps, pack metallic/smoothness, cap runtime maps at 2K."""
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/ThirdParty/Weapons/PISTOLS'
DEST = ROOT / 'Assets/LastSignal/Art/TriplePistols'
SETS = {
    'DXT': ('DXT_ready/2k', 'dxt_Material_Base_color', 'dxt_Material_Normal_OpenGL', 'dxt_Material_Metallic', 'dxt_Material_Roughness', 'dxt_Material_Mixed_AO'),
    'Glock17': ('Glock17/Textures', 'Glock17_BaseColor', 'Glock17_Normal', 'Glock17_Metalness', 'Glock17_Roughness', None),
    'Custom9mm': ('pistol-9mm-custom/textures', 'CustomPistol9mm_BaseColor', 'CustomPistol9mm_Normal', 'CustomPistol9mm_Metallic', 'CustomPistol9mm_Roughness', 'CustomPistol9mml_AO'),
    'CustomMagazine': ('pistol-9mm-custom/textures', 'Magazin_Bullet_Magazine_BaseColor', 'Magazin_Bullet_Magazine_Normal', 'Magazin_Bullet_Magazine_Metallic', 'Magazin_Bullet_Magazine_Roughness', 'Magazin_Bullet_Magazine_AO'),
}
def image(folder, name, mode):
    with Image.open(SOURCE / folder / (name + '.png')) as source:
        return source.convert(mode).resize((2048, 2048), Image.Resampling.LANCZOS)
def author():
    for name, (folder, base, normal, metal, rough, ao) in SETS.items():
        target = DEST / name / 'Textures'; target.mkdir(parents=True, exist_ok=True)
        image(folder, base, 'RGB').save(target / 'BaseColor.png')
        image(folder, normal, 'RGB').save(target / 'Normal.png')
        metallic = image(folder, metal, 'L')
        smoothness = image(folder, rough, 'L').point(lambda value: 255-value)
        zero = Image.new('L', metallic.size, 0)
        Image.merge('RGBA', (metallic, zero, zero, smoothness)).save(target / 'MetallicSmoothness.png')
        if ao: image(folder, ao, 'RGB').save(target / 'AO.png')
if __name__ == '__main__': author()
