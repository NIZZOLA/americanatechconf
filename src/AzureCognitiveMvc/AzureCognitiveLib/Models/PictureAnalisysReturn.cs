using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AzureCognitiveLib.Models
{
    public class PictureAnalisysReturn
    {
        public ImageAnalysis Analysis { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }
    }
}
