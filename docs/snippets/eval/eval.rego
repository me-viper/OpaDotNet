package example

import future.keywords.if

default allow := false

allow if {
    data.password == input.password
}