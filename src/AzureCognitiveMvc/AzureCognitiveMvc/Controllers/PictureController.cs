using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AzureCognitiveMvc.Data;
using AzureCognitiveMvc.Models;
using Microsoft.Extensions.Configuration;
using AzureStorageLib.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Options;
using AzureCognitiveLib.Services;
using AzureCognitiveMvc.ViewModels;
using AzureCognitiveMvc.Utils;

namespace AzureCognitiveMvc.Controllers
{
    public class PictureController : Controller
    {
        private readonly ImageStorageContext _context;
        private readonly IConfiguration _configuration;
        private readonly AzureStorageConfig _azureStorageConfig;
        private readonly string _computerVisionKey;
        private readonly string _computerVisionEndpoint;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Specify the features to return
        private static readonly IList<VisualFeatureTypes?> features =
            new List<VisualFeatureTypes?>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags, VisualFeatureTypes.Adult, VisualFeatureTypes.Objects
        };

        public PictureController(ImageStorageContext context, IOptions<AzureStorageConfig> configOptions,
                                    IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration;
            this._httpContextAccessor = httpContextAccessor;

            _azureStorageConfig = new AzureStorageConfig()
            {
                AccountName = _configuration["AzureStorageConfig:accountName"],
                AccountKey = _configuration["AzureStorageConfig:accountKey"],
                ImageContainer = _configuration["AzureStorageConfig:imageContainer"],
                QueueName = _configuration["AzureStorageConfig:queueName"],
                Url = _configuration["AzureStorageConfig:url"]
            };
            _computerVisionKey = _configuration["ComputerVisionKey"];
            _computerVisionEndpoint = _configuration["ComputerVisionEndpoint"];

        }

        // GET: Pictures
        public async Task<IActionResult> Index()
        {
            string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];
            //read cookie from Request object  
            string cookieValueFromReq = Request.Cookies["Key"];
            //set the key value in Cookie  
            if (cookieValueFromReq == null)
            {
                cookieValueFromReq = Guid.NewGuid().ToString();
                SetCookie("key", cookieValueFromReq, 3600);
            }
            ViewBag.UniqueId = cookieValueFromReq;

            return View(await _context.Pictures.Where(a => a.UserId == Guid.Parse(cookieValueFromReq)).ToListAsync());
        }

        // GET: Pictures
        public async Task<IActionResult> IndexAll()
        {
            string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];
            //read cookie from Request object  
            string cookieValueFromReq = Request.Cookies["Key"];
            //set the key value in Cookie  
            if (cookieValueFromReq == null)
            {
                cookieValueFromReq = Guid.NewGuid().ToString();
                SetCookie("key", cookieValueFromReq, 3600);
            }
            ViewBag.UniqueId = cookieValueFromReq;

            return View(await _context.Pictures.ToListAsync());
        }

        // GET: Pictures/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var picture = await _context.Pictures
                .FirstOrDefaultAsync(m => m.PictureId == id);
            if (picture == null)
            {
                return NotFound();
            }

            PictureIAService picServ = new PictureIAService(_computerVisionEndpoint, _computerVisionKey);

            string url;
            if (picture.Storage)
            {
                url = _azureStorageConfig.Url + _azureStorageConfig.ImageContainer + "/" + picture.Address;
            }
            else
            {
                url = picture.Address;
            }

            var response = await picServ.AnalyzeRemoteAsync(url, features);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);

            ViewBag.ImageUrl = url;
            ViewBag.features = json;


            return View(picture);
        }

        // GET: Pictures/Play/5
        public async Task<IActionResult> Play(int? id, int?[] type)
        {
            var userId = GetCookie("key");

            if (id == null)
            {
                return NotFound();
            }

            IList<VisualFeatureTypes?> featureTypes = new List<VisualFeatureTypes?>();

            if (type.Count() > 0)
            {
                foreach (var itemType in type)
                {
                    var xpto = (VisualFeatureTypes)(Convert.ToInt32(itemType));
                    featureTypes.Add(xpto);
                }
            }
            else
                featureTypes = features;


            var picture = await _context.Pictures.FirstOrDefaultAsync(m => m.PictureId == id);
            if (picture == null)
            {
                return NotFound();
            }

            PictureIAService picServ = new PictureIAService(_computerVisionEndpoint, _computerVisionKey);

            string url;
            if (picture.Storage)
            {
                url = _azureStorageConfig.Url + _azureStorageConfig.ImageContainer + "/" + picture.Address;
            }
            else
            {
                url = picture.Address;
            }

            var response = await picServ.AnalyzeRemoteAsync(url, featureTypes);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(response);

            ViewBag.ImageUrl = url;
            ViewBag.features = json;
            ViewBag.response = response;

            PlayViewModel viewModel = new PlayViewModel();
            viewModel.AnalysisReturn = response;
            viewModel.Picture = picture;

            return View(viewModel);
        }



        // GET: Pictures/Create
        public IActionResult Create()
        {
            var view = new PictureModel();
            view.Storage = true;
            return View(view);
        }

        // POST: Pictures/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PictureId,Description,Address,Storage,Status,Result")] PictureModel picture, IFormFile file)
        {
            var userId = GetCookie("key");

            if (ModelState.IsValid)
            {
                if (picture.Storage)
                {
                    if (file == null || file.Length == 0)
                    {
                        //retorna a viewdata com erro
                        ViewData["Erro"] = "Error: Arquivo(s) não selecionado(s)";
                        return View(ViewData);
                    }
                    else
                    {
                        if (!file.IsImage())
                        {
                            ViewData["Erro"] = "Error: Arquivo enviado não é imagem válida";
                            return View(ViewData);
                        }

                        string filename = System.IO.Path.GetFileName(file.FileName.Replace(" ", ""));
                        var result = await AzureStorageLib.Services.AzureStorageService.UploadFileToStorage(_azureStorageConfig, file.OpenReadStream(), filename);
                        picture.Address = filename;
                        picture.Result = Newtonsoft.Json.JsonConvert.SerializeObject(result);

                    }

                }

                picture.UserId = Guid.Parse(userId);
                _context.Add(picture);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(picture);
        }



        // GET: Pictures/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var picture = await _context.Pictures.FindAsync(id);
            if (picture == null)
            {
                return NotFound();
            }
            return View(picture);
        }

        // POST: Pictures/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PictureId,Description,Address,Storage,Status,Result")] PictureModel picture)
        {
            if (id != picture.PictureId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(picture);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PictureExists(picture.PictureId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(picture);
        }

        // GET: Pictures/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var picture = await _context.Pictures.FirstOrDefaultAsync(m => m.PictureId == id);
            if (picture == null)
            {
                return NotFound();
            }

            return View(picture);
        }

        // POST: Pictures/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var picture = await _context.Pictures.FindAsync(id);
            if (picture.Storage)
            {
                await AzureStorageLib.Services.AzureStorageService.DeleteSpecificBlob(_azureStorageConfig, picture.Address);
            }
            _context.Pictures.Remove(picture);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PictureExists(int id)
        {
            return _context.Pictures.Any(e => e.PictureId == id);
        }

        /// <summary>  
        /// Get the cookie  
        /// </summary>  
        /// <param name="key">Key </param>  
        /// <returns>string value</returns>  
        public string GetCookie(string key)
        {
            return Request.Cookies[key];
        }
        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void SetCookie(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();
            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);
            Response.Cookies.Append(key, value, option);
        }
        /// <summary>  
        /// Delete the key  
        /// </summary>  
        /// <param name="key">Key</param>  
        public void RemoveCookie(string key)
        {
            Response.Cookies.Delete(key);
        }
    }
}
