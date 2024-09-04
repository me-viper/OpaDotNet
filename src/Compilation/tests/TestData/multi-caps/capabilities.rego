package capabilities

f {
  s := custom.zeroArgBuiltin()
  is_string(s)
}

f2 {
  s := custom.otherZeroArgBuiltin()
  is_string(s)
}