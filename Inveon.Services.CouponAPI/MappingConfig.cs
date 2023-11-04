using AutoMapper;
using Inveon.Services.CouponAPI.Models;
using Inveon.Services.CouponAPI.Models.Dtos;

namespace Inveon.Services.CouponAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<CouponDto, Coupon>().ReverseMap();

            });

            return mappingConfig;
        }
    }
}
