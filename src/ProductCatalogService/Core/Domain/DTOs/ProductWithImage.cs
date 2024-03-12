using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs
{
    public class ProductWithImage
    {
        public Guid ProductId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public string CategoryDescription { get; set; }
        public FormFile File { get; set; }
    }
}
