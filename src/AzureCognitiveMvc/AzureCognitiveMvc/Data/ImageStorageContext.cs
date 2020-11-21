using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AzureCognitiveMvc.Models;

namespace AzureCognitiveMvc.Data
{
    public class ImageStorageContext : DbContext
    {
        public ImageStorageContext (DbContextOptions<ImageStorageContext> options)
            : base(options)
        {
        }

        public DbSet<AzureCognitiveMvc.Models.PictureModel> Pictures { get; set; }
    }
}
