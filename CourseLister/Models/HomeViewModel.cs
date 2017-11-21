using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CourseLister.Models
{
    public class HomeViewModel
    {
        public string CanvasAccountId { get; set; }
        
        public string Query { get; set; }
        public bool Authorized { get; set; }
    }
}