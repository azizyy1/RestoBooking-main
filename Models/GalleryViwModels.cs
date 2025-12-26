using System.Collections.Generic;

namespace RestoBooking.Models;

public class GalleryViewModel
{
    public IEnumerable<string> ImageUrls { get; set; } = new List<string>();

    public bool HasUploadedImages { get; set; }

    public string? StatusMessage { get; set; }
}