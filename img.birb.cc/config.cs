public class Config
{
    public string? UserDBPath { get; init; }
    public string? FileDBPath { get; init; }

    private readonly string? salt;
    private List<String>? fileTypes;
}