package example

default hello = false

# METADATA
# entrypoint: true
hello {
    x := input.message
    x == "world"
}