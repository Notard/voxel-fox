"""복셀 여우 생성기: 모델 → 리그 → 애니메이션 → FBX.

    blender --background --factory-startup --python Blender/make_fox.py

출력
    VoxelFox/Assets/Art/Characters/Fox/Fox.fbx
    VoxelFox/Assets/Art/Characters/Fox/Fox_Palette.png
    Blender/Fox.blend

좌표 규칙
    복셀 데이터는 "모델 좌표"(x 오른쪽, y 앞쪽, z 위)로 적는다.
    Blender에서는 여우가 -Y를 바라보게 놓는다(Blender 관례: 정면 = -Y).
    1 복셀 = 0.0625 m × SCALE(0.9) = 0.05625 m, 발바닥 = z 0.
"""
import math
import random
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
FOX_DIR = ROOT / "VoxelFox" / "Assets" / "Art" / "Characters" / "Fox"
FBX_PATH = FOX_DIR / "Fox.fbx"
PALETTE_PATH = FOX_DIR / "Fox_Palette.png"
BLEND_PATH = ROOT / "Blender" / "Fox.blend"

SCALE = 0.9  # 2026-09-29 전체 크기 0.9배 (처음 설계는 1.0)
V = 0.0625 * SCALE  # 복셀 한 칸 크기(m)

# ── 팔레트 ──────────────────────────────────────────────
# 흰 여우 (2026-09-29 붉은 여우에서 변경). 털은 차가운 흰색 3톤,
# 배·볼·주둥이·꼬리 끝은 따뜻한 크림색으로 살짝 구분한다.
PALETTE = [
    (0xF5, 0xF6, 0xF8),  # 0 눈빛 흰색
    (0xFF, 0xFF, 0xFF),  # 1 밝은 흰색
    (0xE2, 0xE6, 0xEC),  # 2 흰색 그늘(푸른 회색)
    (0xFF, 0xF3, 0xE4),  # 3 크림
    (0xF3, 0xE6, 0xD6),  # 4 크림 그늘
    (0x2B, 0x24, 0x20),  # 5 검정 (눈, 코)
    (0xF2, 0xB5, 0xC1),  # 6 귀 안쪽 분홍
    (0xBF, 0xC4, 0xCE),  # 7 연회색 (발끝, 귀 끝)
]
SNOW, SNOW_L, SNOW_D, CREAM, CREAM_S, BLACK, EAR_IN, GRAY = range(8)
PAL_W = 8  # 팔레트 텍스처 8x8, 한 칸 = 한 색

rng = random.Random(7)


def fur(*_):
    return rng.choice((SNOW, SNOW, SNOW, SNOW_L, SNOW_D))


def cream(*_):
    return rng.choice((CREAM, CREAM, CREAM_S))


# ── 복셀 데이터 ─────────────────────────────────────────
# voxels[(x, y, z)] = (part, color). 범위는 [start, end) 반열린 구간.
voxels = {}


def box(part, xr, yr, zr, color):
    for x in range(*xr):
        for y in range(*yr):
            for z in range(*zr):
                voxels[(x, y, z)] = (part, color(x, y, z) if callable(color) else color)


def paint(xr, yr, zr, color):
    """이미 있는 복셀의 색만 바꾼다(파츠 유지)."""
    for x in range(*xr):
        for y in range(*yr):
            for z in range(*zr):
                if (x, y, z) in voxels:
                    part, _ = voxels[(x, y, z)]
                    voxels[(x, y, z)] = (part, color(x, y, z) if callable(color) else color)


# 몸통 6×9×5
box("Body", (-3, 3), (-4, 5), (4, 9), fur)
paint((-2, 2), (-3, 4), (4, 5), cream)  # 배
paint((-2, 2), (4, 5), (4, 7), cream)  # 가슴

# 다리 2×2×4 (털색, 발끝 연회색)
for name, xr, yr in (
    ("Leg_FL", (-3, -1), (2, 4)),
    ("Leg_FR", (1, 3), (2, 4)),
    ("Leg_BL", (-3, -1), (-4, -2)),
    ("Leg_BR", (1, 3), (-4, -2)),
):
    box(name, xr, yr, (0, 4), fur)
    paint(xr, yr, (0, 1), GRAY)

# 머리 6×5×5
box("Head", (-3, 3), (5, 10), (7, 12), fur)
paint((-3, 3), (8, 10), (7, 9), cream)  # 볼
box("Head", (-1, 1), (10, 12), (7, 9), cream)  # 주둥이
paint((-1, 1), (11, 12), (8, 9), BLACK)  # 코
paint((-2, -1), (9, 10), (9, 11), BLACK)  # 눈
paint((1, 2), (9, 10), (9, 11), BLACK)

# 귀 2×2×3 (끝 연회색, 앞면 안쪽 분홍)
for xr, inner in (((-3, -1), (-2, -1)), ((1, 3), (1, 2))):
    box("Head", xr, (6, 8), (12, 15), fur)
    paint(inner, (7, 8), (12, 14), EAR_IN)
    paint(xr, (6, 8), (14, 15), GRAY)

# 꼬리 4×6×4 (끝 크림색)
box("Tail", (-2, 2), (-10, -4), (6, 10), fur)
paint((-2, 2), (-10, -8), (6, 10), cream)

# ── 본 ─────────────────────────────────────────────────
# 모든 본은 위(+Z)를 향한다 → 로컬 X = 월드 X, 로컬 Y = 위, 로컬 Z = 앞.
#   다리: rot X 양수 = 뒤로 스윙, 음수 = 앞으로
#   꼬리: rot X 양수 = 들어올림, rot Y = 좌우 흔들기
#   머리: rot X 양수 = 고개 숙임
#   몸통: loc Y = 위아래
BONES = {  # 이름: (부모, 피벗(모델 좌표, 복셀))
    "Root": (None, (0, 0, 0)),
    "Body": ("Root", (0, 0.5, 6.5)),
    "Head": ("Body", (0, 6, 8)),
    "Leg_FL": ("Body", (-2, 3, 4)),
    "Leg_FR": ("Body", (2, 3, 4)),
    "Leg_BL": ("Body", (-2, -3, 4)),
    "Leg_BR": ("Body", (2, -3, 4)),
    "Tail": ("Body", (0, -4, 8)),
}


def to_blender(p):
    x, y, z = p
    return Vector((x * V, -y * V, z * V))


# ── 메시 생성 ───────────────────────────────────────────
# 블렌더 격자 기준 정육면체 면(바깥에서 봤을 때 반시계 방향)
FACES = {
    (1, 0, 0): ((1, 0, 0), (1, 1, 0), (1, 1, 1), (1, 0, 1)),
    (-1, 0, 0): ((0, 0, 0), (0, 0, 1), (0, 1, 1), (0, 1, 0)),
    (0, 1, 0): ((0, 1, 0), (0, 1, 1), (1, 1, 1), (1, 1, 0)),
    (0, -1, 0): ((0, 0, 0), (1, 0, 0), (1, 0, 1), (0, 0, 1)),
    (0, 0, 1): ((0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)),
    (0, 0, -1): ((0, 0, 0), (0, 1, 0), (1, 1, 0), (1, 0, 0)),
}


def build_mesh():
    # 모델 좌표 칸 (x, y, z) → 블렌더 격자 칸 (x, -y-1, z)
    grid = {(x, -y - 1, z): v for (x, y, z), v in voxels.items()}
    verts, faces, face_color, face_part = [], [], [], []
    for (x, y, z), (part, color) in sorted(grid.items()):
        for (nx, ny, nz), corners in FACES.items():
            neighbor = grid.get((x + nx, y + ny, z + nz))
            # 같은 파츠끼리 맞닿은 면만 지운다. 다른 파츠 경계는 남겨야
            # 관절이 회전해도 구멍이 보이지 않는다.
            if neighbor and neighbor[0] == part:
                continue
            base = len(verts)
            verts += [((x + cx) * V, (y + cy) * V, (z + cz) * V) for cx, cy, cz in corners]
            faces.append((base, base + 1, base + 2, base + 3))
            face_color.append(color)
            face_part.append(part)

    mesh = bpy.data.meshes.new("FoxMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    uv = mesh.uv_layers.new(name="UVMap")
    for poly, color in zip(mesh.polygons, face_color):
        u = (color % PAL_W + 0.5) / PAL_W
        v = 1 - (color // PAL_W + 0.5) / PAL_W
        for li in poly.loop_indices:
            uv.data[li].uv = (u, v)

    obj = bpy.data.objects.new("FoxMesh", mesh)
    bpy.context.scene.collection.objects.link(obj)

    groups = {name: obj.vertex_groups.new(name=name) for name in BONES if name != "Root"}
    for poly, part in zip(mesh.polygons, face_part):
        groups[part].add(list(poly.vertices), 1.0, "REPLACE")  # 강체 스키닝

    print(f"[make_fox] 복셀 {len(voxels)}개, 면 {len(faces)}개, 정점 {len(verts)}개")
    return obj


def build_palette_material(mesh_obj):
    img = bpy.data.images.new("Fox_Palette", PAL_W, PAL_W, alpha=False)
    px = [0.0] * (PAL_W * PAL_W * 4)
    for i, (r, g, b) in enumerate(PALETTE):
        col, row = i % PAL_W, i // PAL_W
        # Blender 이미지 픽셀은 아래 줄부터 저장된다.
        idx = ((PAL_W - 1 - row) * PAL_W + col) * 4
        px[idx:idx + 4] = [r / 255, g / 255, b / 255, 1.0]
    img.pixels = px
    img.filepath_raw = str(PALETTE_PATH)
    img.file_format = "PNG"
    img.save()

    mat = bpy.data.materials.new("FoxMat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0
    mesh_obj.data.materials.append(mat)


def build_armature(mesh_obj):
    arm = bpy.data.armatures.new("FoxRig")
    arm_obj = bpy.data.objects.new("Fox", arm)
    bpy.context.scene.collection.objects.link(arm_obj)
    bpy.context.view_layer.objects.active = arm_obj
    bpy.ops.object.mode_set(mode="EDIT")
    for name, (parent, pivot) in BONES.items():
        eb = arm.edit_bones.new(name)
        eb.head = to_blender(pivot)
        eb.tail = eb.head + Vector((0, 0, 2 * V))
        eb.roll = 0
        if parent:
            eb.parent = arm.edit_bones[parent]
            eb.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")

    mesh_obj.parent = arm_obj
    mod = mesh_obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_obj
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "XYZ"
    return arm_obj


# ── 애니메이션 ─────────────────────────────────────────
TAIL_BASE = 10  # 꼬리 기본 들림 각도


def neutral():
    pose = {name: {"rot": [0, 0, 0], "loc": [0, 0, 0]} for name in BONES}
    pose["Tail"]["rot"][0] = TAIL_BASE
    return pose


def idle_pose(f):
    t = f / 40 * 2 * math.pi
    p = neutral()
    p["Body"]["loc"][1] = 0.35 * (1 - math.cos(t)) / 2  # 숨쉬기
    p["Head"]["rot"][0] = -3 * (1 - math.cos(t)) / 2
    p["Tail"]["rot"][1] = 14 * math.sin(t)  # 좌우 흔들기
    return p


def walk_pose(f):
    t = f / 20 * 2 * math.pi
    swing = 26 * math.cos(t)
    p = neutral()
    # 대각선 다리 쌍: FL+BR / FR+BL
    for name in ("Leg_FL", "Leg_BR"):
        p[name]["rot"][0] = -swing
    for name in ("Leg_FR", "Leg_BL"):
        p[name]["rot"][0] = swing
    p["Body"]["loc"][1] = 0.6 * abs(math.sin(t))  # 한 걸음마다 통통
    p["Head"]["rot"][0] = 2 * math.cos(2 * t)
    p["Tail"]["rot"][0] = TAIL_BASE + 5
    p["Tail"]["rot"][1] = 9 * math.cos(t)
    return p


JUMP_KEYS = {
    # 프레임: {본: (rotX, locY)}  locY는 복셀 단위
    0: {},
    4: {"Body": (0, -0.5), "Leg_FL": (-30, 0), "Leg_FR": (-30, 0),
        "Leg_BL": (30, 0), "Leg_BR": (30, 0), "Head": (6, 0), "Tail": (0, 0)},  # 웅크림
    8: {"Body": (-10, 0.5), "Leg_FL": (-50, 0), "Leg_FR": (-50, 0),
        "Leg_BL": (45, 0), "Leg_BR": (45, 0), "Head": (-8, 0), "Tail": (30, 0)},  # 도약
    13: {"Body": (0, 0.3), "Leg_FL": (-35, 0), "Leg_FR": (-35, 0),
         "Leg_BL": (35, 0), "Leg_BR": (35, 0), "Head": (0, 0), "Tail": (22, 0)},  # 공중
    20: {},  # 착지 준비 → 기본 자세
}


def jump_pose(f):
    p = neutral()
    for name, (rx, ly) in JUMP_KEYS[f].items():
        p[name]["rot"][0] = rx
        p[name]["loc"][1] = ly
    return p


def make_action(arm_obj, name, frames, pose_fn):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = act
    for f in frames:
        pose = pose_fn(f)
        for bname, pb in arm_obj.pose.bones.items():
            pb.location = Vector(pose[bname]["loc"]) * V
            pb.rotation_euler = [math.radians(a) for a in pose[bname]["rot"]]
            pb.keyframe_insert("location", frame=f, group=bname)
            pb.keyframe_insert("rotation_euler", frame=f, group=bname)
    act.use_frame_range = True
    act.frame_start, act.frame_end = frames[0], frames[-1]
    print(f"[make_fox] 액션 {name}: {frames[0]}–{frames[-1]} ({len(frames)} 키)")
    return act


def main():
    FOX_DIR.mkdir(parents=True, exist_ok=True)
    for obj in list(bpy.data.objects):  # 기본 큐브·카메라·조명 제거
        bpy.data.objects.remove(obj, do_unlink=True)
    scene = bpy.context.scene
    scene.render.fps = 30

    mesh_obj = build_mesh()
    build_palette_material(mesh_obj)
    arm_obj = build_armature(mesh_obj)

    make_action(arm_obj, "Idle", list(range(0, 41, 5)), idle_pose)
    make_action(arm_obj, "Walk", list(range(0, 21, 2)), walk_pose)
    make_action(arm_obj, "Jump", sorted(JUMP_KEYS), jump_pose)
    arm_obj.animation_data.action = bpy.data.actions["Idle"]
    scene.frame_start, scene.frame_end = 0, 40

    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        path_mode="STRIP",
    )
    print(f"[make_fox] FBX: {FBX_PATH}")

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))
    print(f"[make_fox] BLEND: {BLEND_PATH}")


main()
