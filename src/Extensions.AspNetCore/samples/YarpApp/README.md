# Yarp Proxy Sample

## Build and Run

```bash
dotnet build
dotnet run
```

## Test

Run:

```bash
curl http://localhost:5079/allow -v
```

Will execute `example/allow` policy and forward you to `example.com`:

Run:

```bash
curl http://localhost:5079/deny -v
```

Will execute `example/deny` policy and output:

```bash
*   Trying 127.0.0.1:5079...
* Connected to localhost (127.0.0.1) port 5079 (#0)
> GET /deny HTTP/1.1
> Host: localhost:5079
> User-Agent: curl/8.0.1
> Accept: */*
>
< HTTP/1.1 403 Forbidden
< Content-Length: 0
< Date: Fri, 18 Aug 2023 09:03:29 GMT
< Server: Kestrel
<
```

Run:

```bash
curl http://localhost:5079/path/allow -v
```

Will execute `example/allow_path` policy and forward you to `example.com`:

Run:

```bash
curl http://localhost:5079/path/any -v
```

Will execute `example/allow_path` policy and output:

```bash
*   Trying 127.0.0.1:5079...
* Connected to localhost (127.0.0.1) port 5079 (#0)
> GET /path/any HTTP/1.1
> Host: localhost:5079
> User-Agent: curl/8.0.1
> Accept: */*
>
< HTTP/1.1 403 Forbidden
< Content-Length: 0
< Date: Fri, 18 Aug 2023 09:05:51 GMT
< Server: Kestrel
<
```
