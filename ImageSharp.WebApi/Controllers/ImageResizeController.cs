using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;

namespace ImageResizer.Controllers
{
    /// <summary>
    /// We can do this peoject like basic of https://kraken.io
    /// </summary>
    [Produces("application/json")]
    [Route("api/ImageResize")]
    public class ImageResizeController : Controller
    {
        private readonly IConfiguration _configuration;
        public ImageResizeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET api/values
        [HttpPost]
        public async Task<ImageSetting> Post([FromBody]ImageSettingInput imageSetting)
        {
            // convert given image url to Uri
            var uri = new Uri(imageSetting.Url);
            // then get original filename without path segments, this way is safer than manual string parsing
            var filename = uri.Segments.Last();

            // hosted base URI, it's better to get that from configuration
            var baseUrl = _configuration["Host"];

            string outputFilename = ""; // file path of original file written under wwwroot
            string outputContentType = ""; // content type of final (converted) image

            // Save method has a remark like that "automatic encoder selected based on extension" so i thought, why am i trying to convert :-)
            // I simply change extension with .png and encoder does the rest
            string pngFilePath = "";
            // converted filename without path segments
            string pngFileName = filename.Replace("jpg", "png");

            // write file from imageSetting.Url, in customer case this file will be a .jpg file
            using (var httpclient = new HttpClient())
            {
                var httpResponseMessage = await httpclient.GetAsync(imageSetting.Url);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    outputFilename = Path.Combine(@"wwwroot\images\", filename);
                    await httpResponseMessage.Content.ReadAsFileAsync(outputFilename, true);
                }
            }

            // again in customer case, read .jpg file, resize it with given scale, then save as .png. i am delegating file type conversion to encoder
            using (Image<Rgba32> image = Image.Load(outputFilename))
            {
                var scale = imageSetting.Scale / 100.0;
                var width = Convert.ToInt32(image.Width * scale);
                var height = Convert.ToInt32(image.Height * scale);

                image.Mutate(x => x.Resize(width, height));
                pngFilePath = outputFilename.Replace("jpg", "png");
                image.Save(pngFilePath); // automatic encoder selected based on extension.

                // this part is also a check, it reads the header of the file (which is a .png) and calculates image's mime type
                // another plus is, now we don't hard code mime types, we directly get that from image's header
                var imageFormat = Image.DetectFormat(pngFilePath);
                outputContentType = imageFormat.DefaultMimeType;
            }

            return new ImageSettingOutput
            {
                Url = baseUrl + "images/" + pngFileName,
                ContentType = outputContentType
            };
        }
    }

    /// <summary>
    /// First we make the ReadAsFileAsync extension method on HttpContent to provide support for reading the content 
    /// and storing it directly in a local file
    /// </summary>
    public static class HttpContentExtensions
    {
        public static Task ReadAsFileAsync(this HttpContent content, string filename, bool overwrite)
        {
            string pathname = Path.GetFullPath(filename);
            if (!overwrite && File.Exists(filename))
            {
                throw new InvalidOperationException(string.Format("File {0} already exists.", pathname));
            }

            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(pathname, FileMode.Create, FileAccess.Write, FileShare.None);
                return content.CopyToAsync(fileStream).ContinueWith(
                    (copyTask) =>
                    {
                        fileStream.Close();
                    });
            }
            catch
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                }

                throw;
            }
        }
    }
}