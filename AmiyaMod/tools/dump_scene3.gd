extends SceneTree

func _init() -> void:
	var ok: bool = ProjectSettings.load_resource_pack("D:/SteamLibrary/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false)
	print("PACK LOADED: ", ok)
	var scene_path := "res://scenes/creature_visuals/living_fog.tscn"
	var txt: String = FileAccess.get_file_as_string(scene_path)
	print("===== living_fog.tscn =====")
	print(txt)
	quit()
