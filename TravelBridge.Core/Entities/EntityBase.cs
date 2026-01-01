namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Base class for all domain entities.
    /// </summary>
    public abstract class EntityBase
    {
        public int Id { get; protected set; }
        public DateTime? DateCreated { get; protected set; }

        protected EntityBase()
        {
            DateCreated = DateTime.UtcNow;
        }
    }
}
