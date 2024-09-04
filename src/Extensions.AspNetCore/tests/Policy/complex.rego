# METADATA
# entrypoint: true
package complex

import future.keywords.if
import future.keywords.in

access if {
    some user in data.complex.users
    user == input.user
}

admin if {
    access
    some u, r in data.complex.roles 
    u == input.user
    r.role == "admin"
}