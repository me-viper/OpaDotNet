package example

default user = false

# METADATA
# entrypoint: true
user {
    user := input.User
    user == "xxx"
}

# METADATA
# entrypoint: true
resource {
    user := input.User
    resource := input.Resource
    
    user == "xxx"
    resource == "10"
}