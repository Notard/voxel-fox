"""여우 미리보기 렌더러 (Workbench).

    blender --background Blender/Fox.blend --python Blender/render_preview.py

출력
    preview/fox/still_<view>.png          정지 이미지 4방향
    preview/fox/<Action>_<frame>.png      애니메이션 프레임 (2프레임 간격)
    preview/fox/manifest.js               미리보기 HTML이 읽는 목록
"""
import json
import math
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "preview" / "fox"
STEP = 2  # 30fps 액션을 2프레임 간격 → 15fps 재생

SCALE = 0.9  # make_fox.py의 SCALE과 같게
CENTER = Vector((0, -0.06, 0.45)) * SCALE
VIEWS = {  # 이름: (카메라 위치, 라벨)
    "front": (Vector((0, -6, 0.6)), "정면"),
    "side": (Vector((6, -0.06, 0.6)), "측면"),
    "three_quarter": (Vector((3.6, -3.8, 2.4)), "3/4"),
    "back": (Vector((-3.6, 3.8, 2.0)), "뒤"),
}
ACTIONS = {  # 이름: (프레임 목록, 루프)
    "Idle": (list(range(0, 40, STEP)), True),
    "Walk": (list(range(0, 20, STEP)), True),
    "Jump": (list(range(0, 21, STEP)), False),
}


def setup_scene():
    scene = bpy.context.scene
    # 흰 여우는 Workbench 스튜디오 조명에서 회색으로 보인다 → EEVEE + 태양광으로
    # Unity(URP Lit + Directional Light)와 비슷한 밝기를 낸다.
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = scene.render.resolution_y = 480
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    # 기본 AgX는 흰색을 회색으로 누른다 → 팔레트 색이 그대로 나오게 Standard
    scene.view_settings.view_transform = "Standard"

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    nodes = world.node_tree.nodes
    bg = nodes.get("Background") or nodes.new("ShaderNodeBackground")
    out = nodes.get("World Output") or nodes.new("ShaderNodeOutputWorld")
    world.node_tree.links.new(bg.outputs["Background"], out.inputs["Surface"])
    bg.inputs["Color"].default_value = (0.72, 0.75, 0.80, 1)  # 푸른 회색 환경광
    bg.inputs["Strength"].default_value = 1.0
    scene.world = world

    sun_data = bpy.data.lights.new("Sun", "SUN")
    sun_data.energy = 3.2  # 흰색(알베도 0.95)이 거의 1.0에 닿는 세기
    sun_data.angle = math.radians(20)  # 부드러운 그림자
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = (math.radians(45), 0, math.radians(40))
    scene.collection.objects.link(sun)

    target = bpy.data.objects.new("CamTarget", None)
    target.location = CENTER
    scene.collection.objects.link(target)

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 1.75 * SCALE
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    track = cam.constraints.new("TRACK_TO")
    track.target = target
    track.track_axis = "TRACK_NEGATIVE_Z"
    track.up_axis = "UP_Y"
    scene.camera = cam
    return scene, cam


def use_action(arm, name):
    ad = arm.animation_data
    act = bpy.data.actions[name]
    ad.action = act
    if hasattr(ad, "action_slot") and act.slots:  # Blender 4.4+ 슬롯 액션
        ad.action_slot = act.slots[0]


def render(scene, path):
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for old in OUT.glob("*.png"):
        old.unlink()
    scene, cam = setup_scene()
    arm = bpy.data.objects["Fox"]

    use_action(arm, "Idle")
    scene.frame_set(0)
    stills = []
    for view, (pos, label) in VIEWS.items():
        cam.location = pos
        render(scene, OUT / f"still_{view}.png")
        stills.append({"file": f"still_{view}.png", "label": label})

    cam.location = VIEWS["three_quarter"][0]
    actions = {}
    for name, (frames, loop) in ACTIONS.items():
        use_action(arm, name)
        files = []
        for f in frames:
            scene.frame_set(f)
            fname = f"{name}_{f:03d}.png"
            render(scene, OUT / fname)
            files.append(fname)
        actions[name] = {"frames": files, "loop": loop, "fps": 30 // STEP}
        print(f"[preview] {name}: {len(files)}프레임")

    manifest = {"stills": stills, "actions": actions}
    (OUT / "manifest.js").write_text(
        "window.FOX_PREVIEW = " + json.dumps(manifest, ensure_ascii=False, indent=1) + ";\n",
        encoding="utf-8",
    )
    print(f"[preview] 완료: {OUT}")


main()
