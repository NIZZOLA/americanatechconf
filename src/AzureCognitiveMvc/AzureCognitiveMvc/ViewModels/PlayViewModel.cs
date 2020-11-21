using AzureCognitiveLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureCognitiveMvc.ViewModels
{
    public class PlayViewModel
    {
        public Models.PictureModel Picture { get; set; }

        public PictureAnalisysReturn AnalysisReturn { get; set; }
    }
}
