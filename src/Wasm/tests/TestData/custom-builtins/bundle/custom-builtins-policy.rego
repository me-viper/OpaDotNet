﻿package custom_builtins

zero_arg = x {
  x = custom.zeroArgBuiltin()
}

one_arg = x {
    x = custom.oneArgBuiltin(input.args[0])
}

one_arg_object = x {
    x = custom.oneArgObjectBuiltin(input.args[0])
}

two_arg = x {
    x = custom.twoArgBuiltin(input.args[0], input.args[1])
}

three_arg = x {
    x = custom.threeArgBuiltin(input.args[0], input.args[1], input.args[2])
}

four_arg = x {
    x = custom.fourArgBuiltin(input.args[0], input.args[1], input.args[2], input.args[3])
}

four_arg_types = x {
    x = custom.fourArgTypesBuiltin(input.args[0], input.args[1], input.args[2], input.args[3])
}

valid_json {
    json.is_valid("{}")
}

json_arg = x {
    x = custom.jsonBuiltin(input.args[0])
}

memorized = [x,y,z] {
    x = custom.memBuiltin("a", 1)
    y = custom.memBuiltin("a", 2)
    z = custom.memBuiltin("a", 1)
}