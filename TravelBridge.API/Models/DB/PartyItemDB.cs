using System.ComponentModel.DataAnnotations;

namespace TravelBridge.API.Models.DB
{
    public class PartyItemDB(int adults, string children, string party) : BaseModel
    {
        public PartyItemDB(PartyItem searchParty) 
            : this(searchParty.adults, searchParty.children != null ? string.Join(',', searchParty.children) : "", searchParty?.party ?? "")
        {
        }

        public int Adults { get; set; } = adults;

        [StringLength(20)]
        public string Children { get; set; } = children;

        [StringLength(100)]
        public string Party { get; set; } = party;

    }
}
