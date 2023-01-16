using System;

public class MapZMetadata {
    // In synthriderz.meta.json
    public int id = -1;
    // In synthriderz.meta.json
    public string hash = "";


    public string FilePath = "";


    public MapZMetadata(int id, string hash, string filePath)
    {
        this.id = id;
        this.hash = hash;
        this.FilePath = filePath;
    }
}