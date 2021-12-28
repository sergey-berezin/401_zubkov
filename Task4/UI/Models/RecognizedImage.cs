using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UI.Models
{
    public class RecognizedImage
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("label")]
        public string Category { get; set; }

        [JsonPropertyName("bBox")]
        public string BBox { get; set; }

        [JsonPropertyName("imageByteData")]
        public byte[] ImageByteData { get; set; }
    }
}
