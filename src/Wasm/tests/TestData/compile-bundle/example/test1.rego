package test1

default hello = false

hello {
    x := input.message
    x == data.test1.world
}