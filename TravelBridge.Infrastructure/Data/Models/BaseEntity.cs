namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Base class for all database entities.
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
