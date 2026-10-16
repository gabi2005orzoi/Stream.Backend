
namespace Stream.Application.DTOs
{
    public class AudioStreamResult
    {
        public System.IO.Stream Stream { get; set; } = null!;
        public string MimeType { get; set; } = "audio/mpeg";
    }
}
