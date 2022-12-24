using System;
using System.Collections.Generic;

namespace NorthwindModel.Models
{
    public partial class Animal
    {
        public int Id { get; set; }
        public string Species { get; set; }
        public string Discriminator { get; set; }
        public decimal? Value { get; set; }
        public string Name { get; set; }
        public string EducationLevel { get; set; }
        public string FavoriteToy { get; set; }
    }
}
