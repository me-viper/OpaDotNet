package samples

import future.keywords.if
import future.keywords.in

invalid_packages[name] := version if {
    some p, v in input
    not is_valid(p, v)
    name := p
    version := v
}

is_valid(pack, ver) if {
    p := trusted_package(pack)
    semver.compare(ver, p.minVersion) >= 0
}

trusted_package(name) := o if {
    some e in data.trustedPackages
    regex.match(e.name, name)
    o := e
}
