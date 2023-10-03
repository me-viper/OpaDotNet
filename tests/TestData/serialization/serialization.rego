package serialization

isArray {
    r := custom.setOrArray(["1", "2"])
    is_array(r)
}

isSet {
    r := custom.setOrArray({"1", "2"})
    is_set(r)
}

retSet {
    r := custom.set()
    is_set(r)
}

retArray {
    r := custom.array()
    is_array(r)
}

charEncoding {
    print(input.s)
    input.s == "?a=1&b=x"
}
