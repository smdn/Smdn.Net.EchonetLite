// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace Smdn.Net.EchonetLite.Appendix;

// use source generation in System.Text.Json
// ref: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation
[JsonSerializable(typeof(SpecificationMaster.SpecificationMasterJsonObject))]
[JsonSerializable(typeof(PropertyMaster))]
internal partial class JsonSerializerSourceGenerationContext : JsonSerializerContext { }
