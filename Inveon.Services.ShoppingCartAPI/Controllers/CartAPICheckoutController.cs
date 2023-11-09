using Inveon.Services.ShoppingCartAPI.Messages;
using Inveon.Services.ShoppingCartAPI.Models.Dto;
using Inveon.Services.ShoppingCartAPI.RabbitMQ;
using Inveon.Services.ShoppingCartAPI.Repository;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Globalization;

namespace Inveon.Services.ShoppingCartAPI.Controllers
{
    [Route("api/cartc")]
    public class CartAPICheckOutController : ControllerBase
    {

        private readonly ICartRepository _cartRepository;
        private readonly ICouponRepository _couponRepository;
        // private readonly IMessageBus _messageBus;
        protected ResponseDto _response;
        private readonly IRabbitMQCartMessageSender _rabbitMQCartMessageSender;
        // IMessageBus messageBus,
        public CartAPICheckOutController(ICartRepository cartRepository,
            ICouponRepository couponRepository, IRabbitMQCartMessageSender rabbitMQCartMessageSender)
        {
            _cartRepository = cartRepository;
            _couponRepository = couponRepository;
            _rabbitMQCartMessageSender = rabbitMQCartMessageSender;
            //_messageBus = messageBus;
            this._response = new ResponseDto();
        }

        [HttpPost]
        [Authorize]
        public async Task<object> Checkout([FromBody] CheckoutHeaderDto checkoutHeader)
        {
            double couponDiscount = 0;
            try
            {
                CartDto cartDto = await _cartRepository.GetCartByUserId(checkoutHeader.UserId);
                if (cartDto == null)
                {
                    return BadRequest();
                }

                if (!string.IsNullOrEmpty(checkoutHeader.CouponCode))
                {
                    CouponDto coupon = await _couponRepository.GetCoupon(checkoutHeader.CouponCode);
                    if (checkoutHeader.DiscountTotal != coupon.DiscountAmount)
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { "Coupon Price has changed, please confirm" };
                        _response.DisplayMessage = "Coupon Price has changed, please confirm";
                        return _response;
                    }
                    couponDiscount = coupon.DiscountAmount;
                }

                checkoutHeader.CartDetails = cartDto.CartDetails;
                //logic to add message to process order.
                // await _messageBus.PublishMessage(checkoutHeader, "checkoutqueue");

                ////rabbitMQ

                Payment payment = OdemeIslemi(checkoutHeader, couponDiscount);
                _rabbitMQCartMessageSender.SendMessage(checkoutHeader, "checkoutqueue");
                await _cartRepository.ClearCart(checkoutHeader.UserId);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }
            return _response;
        }
        public Payment OdemeIslemi(CheckoutHeaderDto checkoutHeaderDto, double couponDiscount)
        {

            CartDto cartDto = _cartRepository.GetCartByUserIdNonAsync(checkoutHeaderDto.UserId);

            Options options = new Options();

            //options.ApiKey = "sandbox-8zkTEIzQ8rikWsvPkL76V8kAvo4DpYuz"; Hocanin iyzico anahtari
            options.ApiKey = "sandbox-HvEkrbc2TSvAcxk3PtEgdAQSI2CCq1W7";
            //options.SecretKey = "sandbox-56FjiYYrjkAuSqENtt0k8b7Ei03s8X61"; Hocanin iyzico anahtari
            options.SecretKey = "sandbox-Cmf9glR0awiuLlWIVhEww8WC8BjPhzJ5";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            CreatePaymentRequest request = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = new Random().Next(1111, 9999).ToString(),

                Currency = Currency.TRY.ToString(),
                Installment = 1,
                BasketId = "B67832"
            };
            request.BasketId = checkoutHeaderDto.CartHeaderId.ToString();
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            PaymentCard paymentCard = new PaymentCard
            {
                CardHolderName = checkoutHeaderDto.CartHeaderId.ToString(),
                CardNumber = checkoutHeaderDto.CardNumber,
                ExpireMonth = checkoutHeaderDto.ExpiryMonth,
                ExpireYear = checkoutHeaderDto.ExpiryYear,
                Cvc = checkoutHeaderDto.CVV,
                RegisterCard = 0,
                CardAlias = "Inveon"
            };
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer
            {
                Id = checkoutHeaderDto.UserId,
                Name = checkoutHeaderDto.FirstName,
                Surname = checkoutHeaderDto.LastName,
                GsmNumber = checkoutHeaderDto.Phone,
                Email = checkoutHeaderDto.Email,
                IdentityNumber = "74300864791",
                LastLoginDate = "2015-10-05 12:43:35",
                RegistrationDate = "2013-04-21 15:12:09",
                RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                Ip = "85.34.78.112",
                City = "Istanbul",
                Country = "Turkey",
                ZipCode = "34732"
            };
            request.Buyer = buyer;

            Address shippingAddress = new Address
            {
                ContactName = $"{checkoutHeaderDto.FirstName} {checkoutHeaderDto.LastName}",
                City = "Istanbul",
                Country = "Turkey",
                Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                ZipCode = "34742"
            };
            request.ShippingAddress = shippingAddress;

            Address billingAddress = new Address
            {
                ContactName = $"{checkoutHeaderDto.FirstName} {checkoutHeaderDto.LastName}",
                City = "Istanbul",
                Country = "Turkey",
                Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1",
                ZipCode = "34742"
            };
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();

            foreach (var cartDetail in checkoutHeaderDto.CartDetails)
            {
                var product = cartDetail.Product;
                BasketItem basketItem = new BasketItem();
                basketItem.Id = "BI10" + product.ProductId.ToString();
                basketItem.Name = product.Name;
                basketItem.Category1 = product.CategoryName;
                basketItem.Price = (product.Price * cartDetail.Count).ToString(CultureInfo.InvariantCulture);
                basketItem.ItemType = BasketItemType.PHYSICAL.ToString();

                basketItems.Add(basketItem);
            }

            request.Price = basketItems.Sum(x=>Convert.ToDecimal(x.Price)).ToString();
            request.PaidPrice = (Convert.ToDouble(request.Price) - couponDiscount).ToString();
            request.BasketItems = basketItems;
            return Payment.Create(request, options);
        }
    }
}
