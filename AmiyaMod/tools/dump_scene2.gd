extends SceneTree

func _init() -> void:
	var ok: bool = ProjectSettings.load_resource_pack("D:/SteamLibrary/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false)
	print("PACK LOADED: ", ok)
	for path in [
		"res://animations/characters/ironclad/ironclad_skel_data.tres",
	]:
		if ResourceLoader.exists(path):
			var txt: String = FileAccess.get_file_as_string(path)
			print("===== ", path, " (len=", txt.length(), ") =====")
			print(txt)
		else:
			print("===== ", path, " NOT FOUND =====")
	# 列出 creature_visuals 目录
	var dir := DirAccess.open("res://scenes/creature_visuals")
	if dir:
		print("===== creature_visuals 目录 =====")
		dir.list_dir_begin()
		var f := dir.get_next()
		while f != "":
			print(f)
			f = dir.get_next()
	quit()
