package example

import future.keywords.contains
import future.keywords.if
import future.keywords.in

default user = false
default resource = false

# METADATA
# entrypoint: true
user if {
    user_has_access
}

# METADATA
# entrypoint: true
resource {
    user_has_access
    user_access_resource
}

user_has_access if data.access.user_access[input.User]

user_access_resource if {
    user_can_access_all_resources
} {
    user_can_access_resource
}

user_can_access_resource if {
    some resource in data.access.user_access[input.User].resources
    resource == input.Resource
}

user_can_access_all_resources if {
    some resource in data.access.user_access[input.User].resources
    resource == "*"
}