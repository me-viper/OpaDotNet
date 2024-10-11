# Web Application Sample

## Build and Run

```bash
dotnet build
dotnet run
```

## Test

Run:

```bash
curl http://localhost:5078/allow -v
```

Will execute `example/allow` policy and output:

```bash
*   Trying 127.0.0.1:5078...
* Connected to localhost (127.0.0.1) port 5078 (#0)
> GET /allow HTTP/1.1
> Host: localhost:5078
> User-Agent: curl/8.0.1
> Accept: */*
>
< HTTP/1.1 200 OK
< Content-Type: text/plain; charset=utf-8
< Date: Fri, 18 Aug 2023 08:50:38 GMT
< Server: Kestrel
< Transfer-Encoding: chunked
<
Hi!
```

Run:

```bash
curl http://localhost:5078/deny -v
```

Will execute `example/deny` policy and output:

```bash
*   Trying 127.0.0.1:5078...
* Connected to localhost (127.0.0.1) port 5078 (#0)
> GET /deny HTTP/1.1
> Host: localhost:5078
> User-Agent: curl/8.0.1
> Accept: */*
>
< HTTP/1.1 403 Forbidden
< Content-Length: 0
< Date: Fri, 18 Aug 2023 08:52:03 GMT
< Server: Kestrel
<
```

Run:

```bash
curl http://localhost:5078/resource/allowed -v
```

Will execute `example/allow` policy and output:

```bash
*   Trying 127.0.0.1:5078...
* Connected to localhost (127.0.0.1) port 5078 (#0)
> GET /resource/allowed HTTP/1.1
> Host: localhost:5078
> User-Agent: curl/8.0.1
> Accept: */*
>
< HTTP/1.1 200 OK
< Content-Type: application/json; charset=utf-8
< Date: Fri, 18 Aug 2023 08:53:04 GMT
< Server: Kestrel
< Transfer-Encoding: chunked
<
"Got access to allowed"
```

Run:

```bash
curl http://localhost:5078/resource/any -v
```

Will execute `example/allow` policy and output:

```bash
*   Trying 127.0.0.1:5078...
* Connected to localhost (127.0.0.1) port 5078 (#0)
> GET /resource/any HTTP/1.1
> Host: localhost:5078
> User-Agent: curl/8.0.1
> Accept: */*
>
< HTTP/1.1 403 Forbidden
< Content-Length: 0
< Date: Fri, 18 Aug 2023 08:53:53 GMT
< Server: Kestrel
<
```
