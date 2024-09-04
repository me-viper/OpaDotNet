package main

import (
	/*
		#include <stdlib.h>
		#include <string.h>

		struct OpaVersion {
			char* libVersion;
			char* goVersion;
			char* commit;
			char* platform;
		};

		struct OpaBuildParams {
			char* target;
			char* capabilitiesJSON;
			char* capabilitiesVersion;
			int bundleMode;
			char** entrypoints;
			int entrypointsLen;
			int debug;
			int optimizationLevel;
			int pruneUnused;
			char* tempDir;
			char* revision;
			char** ignore;
			int ignoreLen;
			int regoVersion;
			int followSymlinks;
			int disablePrintStatements;
		};

		struct OpaFsBuildParams {
			char* source;
			struct OpaBuildParams params;
		};

		struct OpaBytesBuildParams {
			unsigned char* bytes;
			int bytesLen;
			struct OpaBuildParams params;
		};

		struct OpaBuildResult {
			unsigned char* result;
			int resultLen;
			char* errors;
			char* log;
		};
	*/
	"C"
	"bytes"
	"context"
	"fmt"
	"github.com/open-policy-agent/opa/ast"
	"github.com/open-policy-agent/opa/bundle"
	"github.com/open-policy-agent/opa/compile"
	"github.com/open-policy-agent/opa/loader"
	"github.com/open-policy-agent/opa/logging"
	"github.com/open-policy-agent/opa/version"
	"io"
	"io/fs"
	"os"
	"strings"
	"unsafe"
)

func main() {
}

var Vcs string
var defaultCaps *ast.Capabilities

func init() {
	defaultCaps = ast.CapabilitiesForThisVersion()
}

type buildParams struct {
	source                 string
	capabilitiesJSON       string
	capabilitiesVersion    string
	target                 string
	bundleMode             bool
	entrypoints            []string
	debug                  bool
	optimizationLevel      int
	pruneUnused            bool
	revision               string
	fs                     fs.FS
	ignore                 []string
	bundle                 *bundle.Bundle
	regoVersion            ast.RegoVersion
	followSymlinks         bool
	disablePrintStatements bool
}

//export OpaGetVersion
func OpaGetVersion(opaVersion **C.struct_OpaVersion) {
	*opaVersion = (*C.struct_OpaVersion)(C.malloc(C.sizeof_struct_OpaVersion))
	C.memset(unsafe.Pointer(*opaVersion), 0, C.sizeof_struct_OpaVersion)

	(**opaVersion).libVersion = C.CString(version.Version)
	(**opaVersion).goVersion = C.CString(version.GoVersion)
	(**opaVersion).commit = C.CString(Vcs)
	(**opaVersion).platform = C.CString(version.Platform)
}

//export OpaFreeVersion
func OpaFreeVersion(ptr *C.struct_OpaVersion) {
	if ptr == nil {
		return
	}

	C.free(unsafe.Pointer((*ptr).libVersion))
	C.free(unsafe.Pointer((*ptr).goVersion))
	C.free(unsafe.Pointer((*ptr).commit))
	C.free(unsafe.Pointer((*ptr).platform))
	C.free(unsafe.Pointer(ptr))
}

//export OpaBuildFromBytes
func OpaBuildFromBytes(byteParams *C.struct_OpaBytesBuildParams, buildResult **C.struct_OpaBuildResult) int {
	var logger logging.Logger
	loggerBuffer := bytes.NewBuffer(nil)

	if byteParams.params.debug == 0 {
		logger = logging.NewNoOpLogger()
	} else {
		sl := logging.New()
		sl.SetLevel(logging.Debug)
		sl.SetOutput(loggerBuffer)
		logger = sl
	}

	*buildResult = (*C.struct_OpaBuildResult)(C.malloc(C.sizeof_struct_OpaBuildResult))
	C.memset(unsafe.Pointer(*buildResult), 0, C.sizeof_struct_OpaBuildResult)
	logger.Debug("Result pointer: %p", *buildResult)

	buf := C.GoBytes(unsafe.Pointer(byteParams.bytes), byteParams.bytesLen)
	reader := bytes.NewReader(buf)

	ver := ast.RegoVersion(byteParams.params.regoVersion)

	logger.Debug("Rego version: %v", ver)

	b, err := bundle.
		NewReader(reader).
		WithProcessAnnotations(true).
		WithRegoVersion(ver).
		Read()

	if err != nil {
		opaMakeResult(*buildResult, nil, loggerBuffer, err)
		return -3
	}

	logger.Debug("Done reading bundle")

	bp := opaMakeBuildParams(byteParams.params)
	bp.bundle = &b

	logger.Debug("Compiler version: %s", version.Version)
	//logger.Debug("Build params: %v", bp)

	resultBytes, err := opaBuild(bp, loggerBuffer)

	opaMakeResult(*buildResult, resultBytes, loggerBuffer, err)

	if err != nil {
		return -4
	}

	return 0
}

//export OpaBuildFromFs
func OpaBuildFromFs(fsParams *C.struct_OpaFsBuildParams, buildResult **C.struct_OpaBuildResult) int {
	var logger logging.Logger
	loggerBuffer := bytes.NewBuffer(nil)

	if fsParams.params.debug == 0 {
		logger = logging.NewNoOpLogger()
	} else {
		sl := logging.New()
		sl.SetLevel(logging.Debug)
		sl.SetOutput(loggerBuffer)
		logger = sl
	}

	*buildResult = (*C.struct_OpaBuildResult)(C.malloc(C.sizeof_struct_OpaBuildResult))
	C.memset(unsafe.Pointer(*buildResult), 0, C.sizeof_struct_OpaBuildResult)

	bp := opaMakeBuildParams(fsParams.params)
	bp.source = C.GoString(fsParams.source)

	logger.Debug("Compiler version: %s", version.Version)
	//logger.Debug("Build params: %v", bp)

	resultBytes, err := opaBuild(bp, loggerBuffer)

	logger.Debug("Result pointer: %p", *buildResult)

	opaMakeResult(*buildResult, resultBytes, loggerBuffer, err)

	if err != nil {
		return -4
	}

	return 0
}

//export OpaFree
func OpaFree(ptr *C.struct_OpaBuildResult) {
	if ptr == nil {
		return
	}

	C.free(unsafe.Pointer((*ptr).result))
	C.free(unsafe.Pointer((*ptr).errors))
	C.free(unsafe.Pointer((*ptr).log))
	C.free(unsafe.Pointer(ptr))
}

func opaMakeResult(buildResult *C.struct_OpaBuildResult, bytes *bytes.Buffer, log *bytes.Buffer, err error) {
	if log != nil {
		(*buildResult).log = C.CString(log.String())
	}

	if err != nil {
		(*buildResult).errors = C.CString(err.Error())
	}

	if bytes != nil {
		bts := bytes.Bytes()
		(*buildResult).resultLen = C.int(len(bts))
		(*buildResult).result = (*C.uchar)(C.CBytes(bts))
	}
}

func opaMakeBuildParams(params C.struct_OpaBuildParams) *buildParams {
	eps := make([]string, 0, params.entrypointsLen)

	if params.entrypoints != nil {
		var pEps **C.char = params.entrypoints

		for _, entrypoint := range unsafe.Slice(pEps, int(params.entrypointsLen)) {
			ep := C.GoString(entrypoint)

			if len(ep) > 0 {
				eps = append(eps, ep)
			}
		}
	}

	ignore := make([]string, 0, params.ignoreLen)

	if params.ignore != nil {
		var pstr **C.char = params.ignore

		for _, ign := range unsafe.Slice(pstr, int(params.ignoreLen)) {
			s := C.GoString(ign)

			if len(s) > 0 {
				ignore = append(ignore, s)
			}
		}
	}

	return &buildParams{
		capabilitiesJSON:       C.GoString(params.capabilitiesJSON),
		capabilitiesVersion:    C.GoString(params.capabilitiesVersion),
		target:                 C.GoString(params.target),
		bundleMode:             params.bundleMode > 0,
		entrypoints:            eps,
		debug:                  params.debug > 0,
		optimizationLevel:      int(params.optimizationLevel),
		pruneUnused:            params.pruneUnused > 0,
		revision:               C.GoString(params.revision),
		ignore:                 ignore,
		regoVersion:            ast.RegoVersion(params.regoVersion),
		followSymlinks:         params.followSymlinks > 0,
		disablePrintStatements: params.disablePrintStatements > 0,
	}
}

func opaGetCaps(pathOrVersion string, isFile bool) (*ast.Capabilities, error) {
	var result *ast.Capabilities
	var errPath, errVersion error

	if isFile {
		result, errPath = ast.LoadCapabilitiesFile(pathOrVersion)
	} else {
		result, errVersion = ast.LoadCapabilitiesVersion(pathOrVersion)
	}

	if errVersion != nil || errPath != nil {
		return nil, fmt.Errorf("no such file or capabilities version found: %v", pathOrVersion)
	}

	return result, nil
}

func opaMergeCaps(a *ast.Capabilities, b *ast.Capabilities) *ast.Capabilities {
	result := &ast.Capabilities{}
	result.Builtins = append(a.Builtins, b.Builtins...)
	result.Features = append(a.Features, b.Features...)
	result.AllowNet = append(a.AllowNet, b.AllowNet...)
	result.FutureKeywords = append(a.FutureKeywords, b.FutureKeywords...)
	result.WasmABIVersions = append(a.WasmABIVersions, b.WasmABIVersions...)
	return result
}

type loaderFilter struct {
	Ignore []string
}

func (f loaderFilter) Apply(abspath string, info os.FileInfo, depth int) bool {
	for _, s := range f.Ignore {
		if loader.GlobExcludeName(s, 1)(abspath, info, depth) {
			return true
		}
	}
	return false
}

func buildCommandLoaderFilter(bundleMode bool, ignore []string) func(string, os.FileInfo, int) bool {
	return func(abspath string, info os.FileInfo, depth int) bool {
		if !bundleMode {
			if !info.IsDir() && strings.HasSuffix(abspath, ".tar.gz") {
				return true
			}
		}
		return loaderFilter{Ignore: ignore}.Apply(abspath, info, depth)
	}
}

func opaBuild(params *buildParams, loggerBuffer io.Writer) (*bytes.Buffer, error) {
	buf := bytes.NewBuffer(nil)

	var jsonCaps *ast.Capabilities = nil
	var verCaps *ast.Capabilities = nil
	var caps *ast.Capabilities
	var capsErr error

	if len(params.capabilitiesJSON) > 0 {
		jsonCaps, capsErr = ast.LoadCapabilitiesJSON(bytes.NewBufferString(params.capabilitiesJSON))
		if capsErr != nil {
			return nil, capsErr
		}
	}

	if len(params.capabilitiesVersion) > 0 {
		verCaps, capsErr = opaGetCaps(params.capabilitiesVersion, false)
		if capsErr != nil {
			return nil, capsErr
		}
	}

	if jsonCaps == nil && verCaps == nil {
		caps = defaultCaps
	} else {
		if jsonCaps == nil {
			caps = verCaps
		} else if verCaps == nil {
			caps = jsonCaps
		} else {
			caps = opaMergeCaps(jsonCaps, verCaps)
		}
	}

	compiler := compile.New().
		WithTarget(params.target).
		WithAsBundle(params.bundleMode).
		WithEntrypoints(params.entrypoints...).
		WithPaths(params.source).
		WithCapabilities(caps).
		WithEnablePrintStatements(!params.disablePrintStatements).
		WithOutput(buf).
		WithPruneUnused(params.pruneUnused).
		WithOptimizationLevel(params.optimizationLevel).
		WithRegoAnnotationEntrypoints(true).
		WithFilter(buildCommandLoaderFilter(params.bundleMode, params.ignore)).
		WithRegoVersion(params.regoVersion).
		WithFollowSymlinks(params.followSymlinks)

	if params.bundle != nil {
		compiler.WithBundle(params.bundle)
	}

	if params.fs != nil {
		compiler.WithFS(params.fs)
	}

	if params.debug {
		compiler.WithDebug(loggerBuffer)
	}

	if params.revision != "" {
		compiler.WithRevision(params.revision)
	}

	err := compiler.Build(context.Background())

	if err != nil {
		return nil, err
	}

	return buf, nil
}
