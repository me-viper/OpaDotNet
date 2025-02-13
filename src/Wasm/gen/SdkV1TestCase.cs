﻿using System.Text.Json.Nodes;

// ReSharper disable once CheckNamespace
namespace OpaDotNet.Wasm.Tests;

internal class SdkV1TestCase
{
    public string? Skip { get; set; }

    public string Category { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Note { get; set; } = null!;

    public string Query { get; set; } = null!;

    public string[] Modules { get; set; } = null!;

    public JsonArray? WantResult { get; set; }

    public string? WantErrorCode { get; set; }

    public string? WantError { get; set; }

    public bool StrictError { get; set; }

    public JsonNode? Data { get; set; }

    public JsonNode? Input { get; set; }

    public JsonValue? InputTerm { get; set; }

    public bool SortBindings { get; set; }
}