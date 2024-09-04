package az

import future.keywords.if
import future.keywords.in

# METADATA
# entrypoint: true
user if {
    some user in data.simple.users
    user == input.user
}

# METADATA
# entrypoint: true
rq_path if {
    input.path == "/az/request"
}

default attr := false

# METADATA
# entrypoint: true
attr if {
    auth_header := input.headers.Authorization
    startswith(auth_header, "Bearer")
    j := substring(auth_header, count("Bearer "), -1)
    [_, payload, _] := io.jwt.decode(j)
    payload.iss == "opa.tests"
    payload.user == "attrUser"
    payload.role == "attrTester"
}

# METADATA
# entrypoint: true
jwt if {
    auth_header := input.headers.Authorization
    startswith(auth_header, "Bearer")
    j := substring(auth_header, count("Bearer "), -1)
    [_, payload, _] := io.jwt.decode(j)
    payload.iss == "opa.tests"
    payload.user == "jwtUser"
    payload.role == "jwtTester"
}

# METADATA
# entrypoint: true
claims if {
    some claim in input.claims
    claim.type == "claimX"
    claim.value == "valueY"
}

# METADATA
# entrypoint: true
auth_scheme if {
    some claim in input.claims
    claim.type == "scheme"
    claim.value == "Valid"
}