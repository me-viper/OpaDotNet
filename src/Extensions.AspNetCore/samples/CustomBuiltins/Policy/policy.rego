package example

import future.keywords.if

# METADATA
# entrypoint: true
allow_1 if {
    custom1.func(input.path)
}

# METADATA
# entrypoint: true
allow_2 if {
    custom2.func(input.path)
}
