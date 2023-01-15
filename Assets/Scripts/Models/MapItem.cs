using System;

public class MapItem
{
    public int id { get; set; }
    public string hash { get; set; }
    public string title { get; set; }
    public string download_url { get; set; }
    public string published_at { get; set; }
    public string filename { get; set; }
    public DateTime updatedAt { get; set; }
    public User user { get; set; }
}