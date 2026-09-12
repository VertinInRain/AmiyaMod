extends SceneTree

func _init() -> void:
	var ok: bool = ProjectSettings.load_resource_pack("D:/SteamLibrary/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false)
	print("PACK LOADED: ", ok)
	for path in [
		"res://scenes/creature_visuals/ironclad.tscn",
		"res://scenes/creature_visuals/living_smog.tscn",
	]:
		if ResourceLoader.exists(path):
			var txt: String = FileAccess.get_file_as_string(path)
			print("===== ", path, " (len=", txt.length(), ") =====")
			print(txt)
		else:
			print("===== ", path, " NOT FOUND =====")
	quit()
