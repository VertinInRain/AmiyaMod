extends SceneTree

func _init() -> void:
	var ok: bool = ProjectSettings.load_resource_pack("D:/SteamLibrary/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false)
	print("PACK LOADED: ", ok)
	var txt: String = FileAccess.get_file_as_string("res://addons/spine/spine_godot_extension.gdextension")
	print(txt)
	quit()
