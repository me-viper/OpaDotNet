package test2

default hello = false

hello {
    x := input.message
    x == data.test2.world
}