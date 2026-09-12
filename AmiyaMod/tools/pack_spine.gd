extends SceneTree

func _init() -> void:
	var base := "C:/Users/33761/Desktop/mod2/AmiyaMod/assets/spine/amiya3/"
	var packer := PCKPacker.new()
	var err: int = packer.pck_start("C:/Users/33761/Desktop/mod2/AmiyaMod/Amiya.pck")
	if err != OK:
		print("pck_start failed: ", err)
		quit(1)
		return
	for f in ["amiya3.skel", "amiya3.atlas", "amiya3.png", "amiya3_skel_data.tres"]:
		var e: int = packer.add_file("res://mods/Amiya/animations/amiya3/" + f, base + f)
		print("add_file ", f, " -> ", e)
	var ferr: int = packer.flush(true)
	print("flush -> ", ferr)
	quit(0)
