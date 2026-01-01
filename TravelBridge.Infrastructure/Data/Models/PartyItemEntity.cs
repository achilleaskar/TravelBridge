using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Party item entity - stores party composition for a rate.
    /// </summary>
    public class PartyItemEntity : BaseEntity
    {
        public int Adults { get; set; }
        public string Children { get; set; } = "";
        public string Party { get; set; } = "";

        /// <summary>
        /// Creates from Core PartyInfo.
        /// </summary>
        public static PartyItemEntity FromPartyInfo(PartyInfo partyInfo)
        {
            return new PartyItemEntity
            {
                Adults = partyInfo.Adults,
                Children = partyInfo.ChildrenAsString(),
                Party = partyInfo.ToJsonString()
            };
        }

        /// <summary>
        /// Converts to Core PartyInfo.
        /// </summary>
        public PartyInfo ToPartyInfo()
        {
            return PartyInfo.Create(Adults, Children);
        }
    }
}
