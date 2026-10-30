package example

default allow = false

allow {
    input.user == data.admin
}

user := {"name": input.user, "admin": allow}
