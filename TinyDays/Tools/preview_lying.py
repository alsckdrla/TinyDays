"""Fast additive authoring preview from the pre-v106 source; never edits legacy data."""
import bpy, ast, math, sys
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tools'))
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Logs/BeforeV0106/AdultRabbitMotion.blend'))
rig=bpy.data.objects['AdultRig'];base={b.name:b.matrix_local.copy() for b in rig.data.bones}
tree=ast.parse((ROOT/'Tools/animate_adult_rabbit.py').read_text(encoding='utf8'))
for node in tree.body:
    if isinstance(node,ast.FunctionDef) and node.name in ('joint','segment','limb','assign','key'):
        exec(compile(ast.Module(body=[node],type_ignores=[]),'<authoring helpers>','exec'))
from adult_rabbit_lying import build
build(rig,base,assign,limb,key)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Logs/LyingDraft.blend'))
