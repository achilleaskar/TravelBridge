using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Coupon entity - stores discount coupon information.
    /// </summary>
    public class CouponEntity : BaseEntity
    {
        public string Code { get; set; } = "";
        public CouponType CouponType { get; set; }
        public int UsageLimit { get; set; }
        public int UsageLeft { get; set; }
        public int Percentage { get; set; }
        public int Amount { get; set; }
        public DateTime Expiration { get; set; }
    }
}
