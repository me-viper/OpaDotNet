# Changelog

## OpaDotNet.Extensions v2.4.1 (2024-02-12)

### Features

* Bump dependencies

## OpaDotNet.Extensions v2.4.0 (2024-01-30)

### Bug Fixes

* [#6](https://github.com/me-viper/OpaDotNet.Extensions/issues/6). Fix claim serialization ([98ea380](https://github.com/me-viper/OpaDotNet.Extensions/commit/98ea380f12fd879f876b6215428253bd14ceb8f6))

### Features

* Allow creating IHttpRequestPolicyInput instances for extensibility ([39dbc25](https://github.com/me-viper/OpaDotNet.Extensions/commit/39dbc255d82f5121aab5ad224a605af3e7f24455))
* Allow to specify applicable authentication schemes ([e2ce5de](https://github.com/me-viper/OpaDotNet.Extensions/commit/e2ce5de387fc9bccc5f2b6c84e3068579fd884b5))
* Bump OpaDotNet version ([3a72ad2](https://github.com/me-viper/OpaDotNet.Extensions/commit/3a72ad260338675592f74b1d902715141866e2d0))

## OpaDotNet.Extensions v2.3.0 (2024-01-15)

### Bug Fixes

* [#5](https://github.com/me-viper/OpaDotNet.Extensions/issues/5). Fix AuthorizationPolicyProvider composition ([3bf0ad7](https://github.com/me-viper/OpaDotNet.Extensions/commit/3bf0ad762c2f9a512a7b761cc2f01932ed9c7ab6))

## OpaDotNet.Extensions v2.2.0 (2024-01-10)

### Features

* Update dependencies ([0ac41b6](https://github.com/me-viper/OpaDotNet.Extensions/commit/0ac41b6bcedba9b98901bb735ea2a49cfcd6d568))

## OpaDotNet.Extensions v2.1.0 (2023-11-21)

### Features

* [#3](https://github.com/me-viper/OpaDotNet.Extensions/issues/3). Support net8.0 ([c702cba](https://github.com/me-viper/OpaDotNet.Extensions/commit/c702cba2dc32206612ecd4b5d683f42d03bb078c))
* [#4](https://github.com/me-viper/OpaDotNet.Extensions/issues/4). Support precompiled bundles ([d5e5ace](https://github.com/me-viper/OpaDotNet.Extensions/commit/d5e5ace324234d0cbb05b99830b5e76b3092033a))

## OpaDotNet.Extensions v2.0.0 (2023-09-28)

## Bug Fixes

* Fail if configuration has no policies ([948464c](https://github.com/me-viper/OpaDotNet.Extensions/commit/948464c540c3618f38114f27e94f38c40de2a4f9))
* Fix building bundles from configuration ([ada70b6](https://github.com/me-viper/OpaDotNet.Extensions/commit/ada70b6e0ddc23335409d892acd4228949f5c5e7))
* Fix file system monitoring in kubernetes ([4033f54](https://github.com/me-viper/OpaDotNet.Extensions/commit/4033f545815d3e1429b8275d67afdf65d1f06ec4))

## Features

* Add more ServiceCollection helpers ([5c7488f](https://github.com/me-viper/OpaDotNet.Extensions/commit/5c7488f7ffe83581b53cd3ba3f946986ea539416))
* Improve changes detection to avoid unneeded recompilation ([b767367](https://github.com/me-viper/OpaDotNet.Extensions/commit/b7673672e522a3f43023c1ccf1f54910f051299e))
* Improve PolicyHandler extensibility ([8c88c27](https://github.com/me-viper/OpaDotNet.Extensions/commit/8c88c272332729abe5afe19312ca150a3d8eb1b5))
* Update dependencies ([c4ed3d4](https://github.com/me-viper/OpaDotNet.Extensions/commit/c4ed3d42c42ea3f4298ecb79548cc03936d2d4fb))
* Support custom ABI imports ([3f16d5a](https://github.com/me-viper/OpaDotNet.Extensions/commit/3f16d5abd851a53f47bd847153b7d25ebb6817e6))

## OpaDotNet.Extensions v1.1.0 (2023-08-18)

### Features

* Switch to new compilation backend ([b87e99b](https://github.com/me-viper/OpaDotNet.Extensions/commit/b87e99bd025cf271a03519112ed636b8a895f7e6))

## OpaDotNet.Extensions v1.0.5 (2023-08-17)

### Features

* Fix nuget package health warnings

## OpaDotNet.Extensions v1.0.4 (2023-08-16)

### Features

* Update OpaDotNet.Wasm to v1.4.0

## OpaDotNet.Extensions v1.0.3 (2023-07-27)

### Features

* Improve policy sources change tracking
* Update OpaDotNet.Wasm to v1.2.1

## OpaDotNet.Extensions v1.0.2 (2023-07-25)

### Features

* Add JsonOptions configuration ([af93889](https://github.com/me-viper/OpaDotNet.Extensions/commit/af93889905d96be1b5b4ecdd783b3258b2aa4376))