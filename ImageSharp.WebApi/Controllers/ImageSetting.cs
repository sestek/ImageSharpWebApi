namespace ImageResizer.Controllers
{
    public class ImageSetting
    {
        public string Url { get; set; }
    }

    public class ImageSettingInput : ImageSetting
    {
        public int Scale { get; set; }
    }

    public class ImageSettingOutput : ImageSetting
    {
        public string ContentType { get; set; }
    }
}