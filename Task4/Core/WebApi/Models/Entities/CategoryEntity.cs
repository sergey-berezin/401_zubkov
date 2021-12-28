using Core.WebApi.Models.Entities.BaseEntities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;



namespace Core.WebApi.Models.Entities
{
    public class CategoryEntity : Entity
    {
        [Required] public string CategoryName { get; set; }
        public virtual ICollection<RecognizedImageEntity> Images { get; set; }
    }
}
