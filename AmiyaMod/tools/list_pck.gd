extends SceneTree

func _init() -> void:
	var pck_path: String = "D:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/Typhon/Typhon.pck"
	var ok: bool = ProjectSettings.load_resource_pack(pck_path, false)
	print("PACK LOADED: ", ok)
	if not ok:
		quit(1)
		return
	_list_dir("res://", 0)
	quit(0)

func _list_dir(path: String, depth: int) -> void:
	if depth > 6:
		return
	var dir := DirAccess.open(path)
	if dir == null:
		return
	dir.list_dir_begin()
	var f := dir.get_next()
	while f != "":
		if f == "." or f == "..":
			f = dir.get_next()
			continue
		var full := path + f
		if dir.current_is_dir():
			print("  ".repeat(depth) + full + "/")
			_list_dir(full + "/", depth + 1)
		else:
			print("  ".repeat(depth) + full)
		f = dir.get_next()
