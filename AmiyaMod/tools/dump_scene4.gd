extends SceneTree

func _init() -> void:
	var ok: bool = ProjectSettings.load_resource_pack("D:/SteamLibrary/steamapps/common/Slay the Spire 2/SlayTheSpire2.pck", false)
	print("PACK LOADED: ", ok)
	var data: PackedByteArray = FileAccess.get_file_as_bytes("res://animations/characters/ironclad/ironclad.skel")
	print("ironclad.skel bytes: ", data.size())
	var f := FileAccess.open("C:/Users/33761/AppData/Roaming/SlayTheSpire2/amiya_test/ironclad.skel", FileAccess.WRITE)
	if f:
		f.store_buffer(data)
		f.close()
		print("SAVED")
	else:
		print("SAVE FAILED: ", FileAccess.get_open_error())
	quit()
