extends SceneTree

func _init():
  var seed_val := 12345
  var region_count := 5

  var args := OS.get_cmdline_args()
  for i in range(args.size()):
          if args[i] == "--seed" and i + 1 < args.size():
                  seed_val = int(args[i + 1])
          if args[i] == "--regions" and i + 1 < args.size():
                  region_count = int(args[i + 1])

  var Gen = preload("res://scripts/core/sim/galaxy_generator.gd")
  var gen1 = Gen.new(seed_val)
  var gen2 = Gen.new(seed_val)

  var d1 := gen1.determinism_digest(region_count)
  var d2 := gen2.determinism_digest(region_count)


  print("seed=%s regions=%s" % [seed_val, region_count])
  print("digest_1=%s" % d1)
  print("digest_2=%s" % d2)

  if d1 != d2:
          printerr("DETERMINISM_FAIL: digest mismatch")
          quit(1)
          return

  print("DETERMINISM_OK")
  quit(0)

