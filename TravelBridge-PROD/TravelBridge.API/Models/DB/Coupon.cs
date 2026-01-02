using System.ComponentModel.DataAnnotations;

namespace TravelBridge.API.Models.DB
{
    public class Coupon : BaseModel
    {
        [MaxLength(50)]
        public string Code { get; set; }

        public CouponType CouponType { get; set; }

        public int UsageLimit { get; set; }
        public int UsageLeft { get; set; }

        public int Percentage { get; set; }
        public int Amount { get; set; }

        public DateTime Expiration { get; set; }
    }
}
