﻿#nullable disable // JSON + nullable sucks...
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Slate.FakeCDN.Models
{
    public class Manifest : ManifestMetadata
    {
        [JsonPropertyName("files")]
        public List<ManifestFile> Files { get; set; }
    }
}
