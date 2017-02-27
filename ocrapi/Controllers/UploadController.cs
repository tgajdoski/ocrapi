using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Cors;
using PdfToImage;
using Tesseract;



namespace ocrapi.Controllers

{

    [EnableCors(origins: "*", headers: "*", methods: "*")] // tune to your needs
    [RoutePrefix("api/Upload")]
    public class UploadController : ApiController
    {

        PDFConvert converter = new PDFConvert();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [EnableCors(origins: "*", headers: "*", methods: "*")] 
        [Route("user/PostUserImage")]
  

        public async Task<HttpResponseMessage> PostUserImage()
        {


            //HttpRequestMessage request = this.Request;
            //if (!request.Content.IsMimeMultipartContent())
            //{
            //    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            //}


            Dictionary<string, object> dict = new Dictionary<string, object>();
            try
            {

                var httpRequest = HttpContext.Current.Request;

                foreach (string file in httpRequest.Files)
                {
                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created);
                    response.Headers.Add("Access-Control-Allow-Origin", "true");

                    var postedFile = httpRequest.Files[file];
                    var extension = "";
                    var message1 = "";
                    if (postedFile != null && postedFile.ContentLength > 0)
                    {

                        int MaxContentLength = 1024 * 1024 * 1; //Size = 1 MB  

                        IList<string> AllowedFileExtensions = new List<string> { ".jpg", ".gif", ".png", ".pdf" };
                        var imaextenzija = postedFile.FileName.LastIndexOf('.');
                        var ext = ".png";
                        if (imaextenzija != -1)
                             ext = postedFile.FileName.Substring(postedFile.FileName.LastIndexOf('.'));
                        extension = ext.ToLower();
                        if (!AllowedFileExtensions.Contains(extension))
                        {

                            var message = string.Format("Please Upload image of type .jpg,.gif,.png or .pdf");

                            dict.Add("error", message);
                            return Request.CreateResponse(HttpStatusCode.BadRequest, dict);
                        }
                        else if (postedFile.ContentLength > MaxContentLength)
                        {

                            var message = string.Format("Please Upload a file upto 1 mb.");

                            dict.Add("error", message);
                            
                            return Request.CreateResponse(HttpStatusCode.BadRequest, dict);
                        }
                        else
                        {



                            var filePath = HttpContext.Current.Server.MapPath("~/Uploads/" + postedFile.FileName + extension);

                            postedFile.SaveAs(filePath);

                        }
                    }

                    if (extension == ".pdf")
                    {
                        // convert to image and ocr-it
                        string filenametemp = postedFile.FileName;
                        var filePath = HttpContext.Current.Server.MapPath("~/Uploads/" + postedFile.FileName + extension);
                        string outputname = HttpContext.Current.Server.MapPath("~/Uploads/pdf2tif/") + filenametemp.Substring(0, filenametemp.Length - 4) + ".tif";
                        message1 = ConvertSingleImage(filePath, outputname);
                    }
                    else
                    {
                        var filePath = HttpContext.Current.Server.MapPath("~/Uploads/" + postedFile.FileName + extension);
                        // ocr-image
                        using (var engine = new TesseractEngine(HttpContext.Current.Server.MapPath(@"~/tessdata"), "eng", EngineMode.Default))
                        {
                            // have to load Pix via a bitmap since Pix doesn't support loading a stream.
                            using (var image = new System.Drawing.Bitmap(filePath))
                            {
                                using (var pix = PixConverter.ToPix(image))
                                {
                                    using (var page = engine.Process(pix))
                                    {
                                    //    meanConfidenceLabel.InnerText = String.Format("{0:P}", page.GetMeanConfidence());
                                        message1 = page.GetText() + " %" + page.GetMeanConfidence();
                                    }
                                }
                            }
                        }
                    }


                    
                    return Request.CreateErrorResponse(HttpStatusCode.Created, message1); ;
                }
                var res = string.Format("Please Upload a image.");
                dict.Add("error", res);
                return Request.CreateResponse(HttpStatusCode.NotFound, dict);
            }
            catch (Exception ex)
            {
                var res = string.Format("some Messag e " + ex.ToString());
                dict.Add("error", res);
                return Request.CreateResponse(HttpStatusCode.NotFound, dict);
            }
        }




        private string  ConvertSingleImage(string filename, string outputname)
        {

            var message1 = "";
            bool Converted = false;
            //Setup the converter

            converter.RenderingThreads = -1;

            converter.TextAlphaBit = -1;

            converter.TextAlphaBit = -1;
            converter.OutputToMultipleFile = false;
            converter.FirstPageToConvert = -1;
            converter.LastPageToConvert = -1;
            converter.FitPage = false;
            converter.JPEGQuality = 10;
            converter.OutputFormat = "tifflzw";
            System.IO.FileInfo input = new System.IO.FileInfo(filename);
            //  string output = string.Format("{0}\\{1}{2}", input.Directory, input.Name, txtExtension.Text);
            //If the output file exist alrady be sure to add a random name at the end until is unique!

            Converted = converter.Convert(input.FullName, outputname);

            if (Converted)
            {

        
                // ocr-image
                using (var engine = new TesseractEngine(HttpContext.Current.Server.MapPath(@"~/tessdata"), "eng", EngineMode.Default))
                {
                    // have to load Pix via a bitmap since Pix doesn't support loading a stream.
                    using (var image = new System.Drawing.Bitmap(outputname))
                    {
                        using (var pix = PixConverter.ToPix(image))
                        {
                            using (var page = engine.Process(pix))
                            {
                                //    meanConfidenceLabel.InnerText = String.Format("{0:P}", page.GetMeanConfidence());
                                message1 = page.GetText() + " %" + page.GetMeanConfidence();
                            }
                        }
                    }
                }

            }
            else
            {
                message1 = string.Format("{0}:File NOT converted! Check Args!", DateTime.Now.ToShortTimeString());

            }

            return message1;
        }
    }
}