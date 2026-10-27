using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lab_BE1_2_25DH190025.Models.ViewModels
{
    public class ProductSearchVM
    {
        public string SearchTerm { get; set; }
        public List<Product> Products { get; set; }
        public ProductSearchVM() { }
        public ProductSearchVM(string searchTerm) {
            SearchTerm = searchTerm;
            Products = new List<Product>();
        }
    }
}