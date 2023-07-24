# METADATA
# title: Metadata Package
# description: A set of rules illustrating how metadata annotations can be merged.
# authors:
# - John Doe <john@example.com>
# organizations:
# - Acme Corp.
package example

a := 1

# METADATA
# title: Deny invalid numbers
# description: Numbers may not be higher than 5
# custom:
#  severity: MEDIUM
deny[format(rego.metadata.rule())] {
    input.number > 5
}

# METADATA
# title: Deny non-admin subjects
# description: Subject must have the 'admin' role
# custom:
#  severity: HIGH
deny[format(rego.metadata.rule())] {
    input.role != "admin"
}

format(meta) := {"severity": meta.custom.severity, "reason": meta.description}