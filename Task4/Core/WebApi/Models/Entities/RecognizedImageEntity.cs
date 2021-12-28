using Core.WebApi.Models.Entities.BaseEntities;


namespace Core.WebApi.Models.Entities
{
    public class RecognizedImageEntity : Entity
    {
        public virtual CategoryEntity CategoryEntity { get; set; }
        public string BBox { get; set; }
        public virtual byte[] SerializedImage { get; set; }
    }
}
