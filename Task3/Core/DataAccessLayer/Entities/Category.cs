using Core.DataAccessLayer.Entities.BaseEntities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Core.DataAccessLayer.Entities
{
    public class Category : Entity
    {
        [Required] public string CategoryName { get; set; }
        public ICollection<RecognizedImage> Images { get; set; }
    }
}
