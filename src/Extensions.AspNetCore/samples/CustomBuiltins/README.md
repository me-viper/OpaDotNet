# Web Application With Custom Builtins Sample

## Build and Run

```bash
dotnet build
dotnet run
```

## Test

Run:

```bash
curl http://localhost:5078/allow1 -v
```

Will execute `example/allow_1` policy and output:

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
Custom built-in 1
```

Run:

```bash
curl http://localhost:5078/allow2 -v
```

Will execute `example/allow_2` policy and output:

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
Custom built-in 2
```
