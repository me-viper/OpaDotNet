package http_in

import future.keywords.if
import future.keywords.in

# METADATA
# entrypoint: true
claim_value_array if {
    print(input)
    some claim in input.claims
    claim.type == "role"
    some role in claim.value
    role == "test"
}