using System;
using Newtonsoft.Json;

public class MapItem
{
    public int id { get; set; }
    public string hash { get; set; }
    public string title { get; set; }
    public string download_url { get; set; }
    public string published_at { get; set; }
    public string created_at { get; set; }
    public string uploaded_at {get; set; }
    public string filename { get; set; }
    public User user { get; set; }

    public DateTime? GetPublishedAtUtc() {
        // Example: 2023-01-20T04:54:13.807Z, given in UTC
        DateTime publishedAtUtc;
        if (DateTime.TryParse(published_at, out publishedAtUtc)) {
            return publishedAtUtc;
        } else {
            return null;
        }
    }
}