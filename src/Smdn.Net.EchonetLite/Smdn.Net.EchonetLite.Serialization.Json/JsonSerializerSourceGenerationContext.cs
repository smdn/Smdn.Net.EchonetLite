// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

using Smdn.Net.EchonetLite.Protocol;

namespace Smdn.Net.EchonetLite.Serialization.Json;

// use source generation in System.Text.Json
// ref: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
[JsonSerializable(typeof(Format1Message))]
internal partial class JsonSerializerSourceGenerationContext : JsonSerializerContext { }
