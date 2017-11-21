using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CourseLister.Models
{
    public class HomeViewModel
    {
        [Required]
        [Display(Name = "Canvas Account Id")]
        public string CanvasAccountId { get; set; }
        
        [Required]
        [Display(Name = "Canvas Query")]
        public string Query { get; set; }
        public bool Authorized { get; set; }
    }
}