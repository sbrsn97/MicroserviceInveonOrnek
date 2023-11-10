using Inveon.Services.CouponAPI.Models.Dtos;
using Inveon.Services.CouponAPI.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inveon.Services.CouponAPI.Controllers
{
    [ApiController]
    [Route("api/coupon")]
    public class CouponAPIController : Controller
    {
        private readonly ICouponRepository _couponRepository;
        protected ResponseDto _response;

        public CouponAPIController(ICouponRepository couponRepository)
        {
            _couponRepository = couponRepository;
            this._response = new ResponseDto();
        }

        [HttpGet("{code}")]
        public async Task<object> GetDiscountForCode(string code)
        {
            try
            {
                var coupon = await _couponRepository.GetCouponByCode(code);
                _response.Result = coupon;
                _response.IsSuccess = true;
                if (coupon is null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "This coupon code is not valid." };
                }
            }
            catch (Exception ex)
            {
                
            }
            return _response;
        }
    }
}
