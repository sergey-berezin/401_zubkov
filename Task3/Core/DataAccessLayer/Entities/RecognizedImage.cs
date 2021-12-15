using Core.DataAccessLayer.Entities.BaseEntities;


namespace Core.DataAccessLayer.Entities
{
    public class RecognizedImage : Entity
    {
        public Category Category { get; set; }
        public string BBox { get; set; }
        public virtual byte[] SerializedImage { get; set; }
    }
}
