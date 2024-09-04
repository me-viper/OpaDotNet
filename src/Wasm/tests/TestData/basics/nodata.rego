package example

default hello = false

hello {
    x := input.message
    x == "world"
}