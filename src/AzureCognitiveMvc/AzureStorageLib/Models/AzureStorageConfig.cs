using System;
using System.Collections.Generic;
using System.Text;

namespace AzureStorageLib.Models
{
    public class AzureStorageConfig
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string QueueName { get; set; }
        public string ImageContainer { get; set; }
        public string ThumbnailContainer { get; set; }
        public string Url { get; set; }
        
    }
}
