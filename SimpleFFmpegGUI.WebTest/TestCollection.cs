namespace SimpleFFmpegGUI.WebTest;

public class TestCollection
{
    [CollectionDefinition("FFmpegWebCollection")]
    public class FFmpegWebCollection : ICollectionFixture<SimpleFFmpegWebApplicationFactory>
    {
    }
}