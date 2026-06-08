package http_in

import future.keywords.if
import future.keywords.in

ca := `-----BEGIN CERTIFICATE-----
MIIDBTCCAe2gAwIBAgIUH5CI2p8SoIZ8zOc91Mq5zcQdS5kwDQYJKoZIhvcNAQEL
BQAwETEPMA0GA1UEAwwGbXktb3JnMCAXDTI2MDYwODA1NTU0MVoYDzIxMjYwNTE1
MDU1NTQxWjARMQ8wDQYDVQQDDAZteS1vcmcwggEiMA0GCSqGSIb3DQEBAQUAA4IB
DwAwggEKAoIBAQCfz4juPSLbl7thWLPL8w+F2iNFCjlcR7JEdoE26KqUj6/jxrH+
yFkcoiz28b/Y5pBJfJ6IuABBrs8A9lR9G3LiMzIREg5UzsW4Tg7iwN4ixpYM74N8
S3OdUCdrjnb96mDODzYFy45qxcwvLh0HiJF/lYicC2B6+42uZQ3i1xf3xjYFl8db
UXS9ZAZluGr9Y+HjdO1ZDuCWsmbRZvSHDdZ4DWJIz9FB/lAVG48DhxPbRacHEViy
wc1u+gYIDl2myEz8gmpf1buopFEmR62koexILJLUeYcePZEMc6BKFKKATje32JN4
qq0j36QTYuhhIQrrmhmcVZfcEboN+08BXhihAgMBAAGjUzBRMB0GA1UdDgQWBBQA
/bZCYY3qMYpwj3EqYFSF9BpEYDAfBgNVHSMEGDAWgBQA/bZCYY3qMYpwj3EqYFSF
9BpEYDAPBgNVHRMBAf8EBTADAQH/MA0GCSqGSIb3DQEBCwUAA4IBAQBFt9oCZmdr
VabEDuFU0HckUHwnJ2nzGuhYTPt4yx9MHX5mD9prvWqgRqh9LALVrD0JI+Pcscwg
/nSdKEakeZAVp93nZZzQAIjbSNL2fMtVJkyDJ/AZDgC73NoSkJRGzpG6K7kKsgqz
4iKa2gat8OE6sCAa4A9VbJKlgLYOphgD8KS9nUB916RvevooNJfgu2ef31nYEtla
o5Ca3M0NyW60OBGfxuLnz4Akkr+ihNKUiUy9bpDfHszJf0+uzlgrOM4oDUl+6Q2m
joIpf43RUwb0xvU1VWN7aF+iqkgvycrcP3C1iujrBilkEjgGIatb5I1JwjfJk/NR
FmpJrxCUAl0V
-----END CERTIFICATE-----`

# METADATA
# entrypoint: true
client_cert if {
    certs := concat("\n", [ca, input.connection.clientCertificatePem])
    print(certs)
    result := crypto.x509.parse_and_verify_certificates(certs)
    result[0]
}