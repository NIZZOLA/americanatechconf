using AzureCognitiveLib.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AzureCognitiveLib.Services
{
    public class PictureIAService
    {
        private ComputerVisionClient _computerVision;

        public PictureIAService(string endPoint, string subscriptionKey)
        {
            _computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey))
            { Endpoint = endPoint };
        }

        // Analyze a remote image
        public async Task<PictureAnalisysReturn> AnalyzeRemoteAsync(string imageUrl, IList<VisualFeatureTypes?> features)
        {
            try
            {

                var response = new PictureAnalisysReturn();
                if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                {
                    response.Message = string.Format("Endereço remoto inválido: {0} ", imageUrl);
                    return response;
                }
                ImageAnalysis analysis = await _computerVision.AnalyzeImageAsync(imageUrl, features);
                response.Analysis = analysis;
                Console.WriteLine(JsonSerializer.Serialize(analysis));
                return response;
            }
            catch (Exception error)
            {
                //TODO- tratar erro
                throw;
            }
        }

        // Analyze a local image
        public async Task<PictureAnalisysReturn> AnalyzeLocalAsync(string imagePath, IList<VisualFeatureTypes?> features)
        {
            var response = new PictureAnalisysReturn();
            if (!File.Exists(imagePath))
            {
                response.Message = string.Format("Não é possível abrir ou ler o arquivo {0}", imagePath);
                return response;
            }
            using (Stream imageStream = File.OpenRead(imagePath))
            {
                ImageAnalysis analysis = await _computerVision.AnalyzeImageInStreamAsync(imageStream, features);
                response.FileName = imagePath;
                response.Analysis = analysis;
                return response;
            }
        }
    }
}
