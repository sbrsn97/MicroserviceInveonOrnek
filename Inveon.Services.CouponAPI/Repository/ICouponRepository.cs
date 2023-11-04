using Inveon.Services.CouponAPI.Models.Dtos;

namespace Inveon.Services.CouponAPI.Repository
{
    public interface ICouponRepository
    {
        Task<CouponDto> GetCouponByCode(string couponCode);
    }
}
