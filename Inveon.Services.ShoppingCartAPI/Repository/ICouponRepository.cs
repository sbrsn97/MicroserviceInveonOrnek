using Inveon.Services.ShoppingCartAPI.Models.Dto;

namespace Inveon.Services.ShoppingCartAPI.Repository
{
    public interface ICouponRepository
    {
        Task<CouponDto> GetCoupon(string couponName);
    }
}
