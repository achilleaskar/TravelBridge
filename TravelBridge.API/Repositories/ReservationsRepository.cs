using Microsoft.EntityFrameworkCore;
using TravelBridge.API.Contracts;
using TravelBridge.API.DataBase;
using TravelBridge.API.Models.DB;

namespace TravelBridge.API.Repositories
{
    public class ReservationsRepository
    {
        private readonly AppDbContext db;
        private readonly ILogger<ReservationsRepository> _logger;

        public ReservationsRepository(AppDbContext db, ILogger<ReservationsRepository> logger)
        {
            this.db = db;
            _logger = logger;
        }

        internal async Task CreateTemporaryExternalReservation(CheckoutResponse res, Endpoints.ReservationEndpoints.BookingRequest pars, DateTime parsedCheckin, DateTime parsedCheckOut, TravelBridge.Payments.Viva.Models.ExternalModels.VivaPaymentRequest payment, string orderCode, string party)
        {
            _logger.LogInformation("CreateTemporaryExternalReservation started for HotelId: {HotelId}, OrderCode: {OrderCode}, TotalPrice: {TotalPrice}", 
                pars.HotelId, orderCode, res.TotalPrice);

            try
            {
                var newRes = new Reservation
                {
                    CheckIn = DateOnly.FromDateTime(parsedCheckin.Date),
                    CheckOut = DateOnly.FromDateTime(parsedCheckOut.Date),
                    HotelCode = pars.HotelId,
                    HotelName = res.HotelData.Name,
                    TotalAmount = res.TotalPrice,
                    TotalRooms = res.Rooms.Count,
                    Party = party,
                    Coupon = pars.couponCode,
                    CheckInTime = res.CheckInTime,
                    CheckOutTime = res.CheckOutTime,
                    BookingStatus = BookingStatus.Pending,
                    Customer = new Customer(pars.CustomerInfo?.FirstName, pars.CustomerInfo?.LastName, pars.CustomerInfo?.Phone, "GR", pars.CustomerInfo?.Email, pars.CustomerInfo?.Requests),
                    Payments = new List<Payment>
                    {
                        new() {
                            Amount = pars.PrepayAmount??throw new InvalidDataException("Invalid Payment Amount"),
                            OrderCode = orderCode,
                            PaymentProvider=PaymentProvider.Viva,
                            PaymentStatus= PaymentStatus.Pending
                        }
                    },
                    PartialPayment = new PartialPaymentDB(res.PartialPayment, res.TotalPrice),
                    RemainingAmount = res.TotalPrice - res.PartialPayment?.prepayAmount ?? res.TotalPrice,
                    Rates = res.Rooms.Select(r => new ReservationRate
                    {
                        HotelCode = pars.HotelId,
                        Price = r.TotalPrice * r.SelectedQuantity,
                        Name = r.RoomName,
                        Provider = Provider.WebHotelier,
                        BookingStatus = BookingStatus.Pending,
                        RateId = r.RateId,
                        NetPrice = r.NetPrice * r.SelectedQuantity,
                        Quantity = r.SelectedQuantity,
                        SearchParty = new(r.RateProperties.SearchParty),
                        CancelationInfo = r.RateProperties.CancellationName,
                        BoardInfo = r.RateProperties.Board,
                    }).ToList()
                };

                await db.Reservations.AddAsync(newRes);
                await db.SaveChangesAsync();

                _logger.LogInformation("CreateTemporaryExternalReservation completed - ReservationId: {ReservationId}, HotelId: {HotelId}, OrderCode: {OrderCode}, RatesCount: {RatesCount}", 
                    newRes.Id, pars.HotelId, orderCode, newRes.Rates.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateTemporaryExternalReservation failed for HotelId: {HotelId}, OrderCode: {OrderCode}", 
                    pars.HotelId, orderCode);
                throw new Exception(ex.Message);
            }
        }

        internal async Task<Reservation?> GetFullReservationFromPaymentCode(string orderCode)
        {
            _logger.LogDebug("GetFullReservationFromPaymentCode started for OrderCode: {OrderCode}", orderCode);

            var reservation = await db.Reservations
                .Where(r => r.Payments.Any(p => p.OrderCode == orderCode))
                .Include(r => r.Rates).ThenInclude(r => r.SearchParty)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync();

            if (reservation != null)
            {
                _logger.LogDebug("GetFullReservationFromPaymentCode found ReservationId: {ReservationId} for OrderCode: {OrderCode}", 
                    reservation.Id, orderCode);
            }
            else
            {
                _logger.LogWarning("GetFullReservationFromPaymentCode: No reservation found for OrderCode: {OrderCode}", orderCode);
            }

            return reservation;
        }

        internal async Task<Reservation?> GetReservationBasicDataByPaymentCode(string orderCode)
        {
            _logger.LogDebug("GetReservationBasicDataByPaymentCode started for OrderCode: {OrderCode}", orderCode);

            var reservation = await db.Reservations
                .Where(r => r.Payments.Any(p => p.OrderCode == orderCode))
                .Include(r => r.Customer)
                .Include(r => r.PartialPayment).ThenInclude(p => p.nextPayments)
                .Include(r => r.Payments)
                .Include(r => r.Rates).ThenInclude(p => p.SearchParty)
                .FirstOrDefaultAsync();

            if (reservation != null)
            {
                _logger.LogDebug("GetReservationBasicDataByPaymentCode found ReservationId: {ReservationId} for OrderCode: {OrderCode}", 
                    reservation.Id, orderCode);
            }
            else
            {
                _logger.LogWarning("GetReservationBasicDataByPaymentCode: No reservation found for OrderCode: {OrderCode}", orderCode);
            }

            return reservation;
        }

        internal async Task<bool> UpdatePaymentSucceed(string orderCode, string tid)
        {
            _logger.LogInformation("UpdatePaymentSucceed started for OrderCode: {OrderCode}, Tid: {Tid}", orderCode, tid);

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderCode == orderCode);
            if (payment?.PaymentStatus == PaymentStatus.Pending)
            {
                payment.PaymentStatus = PaymentStatus.Success;
                payment.DateFinalized = DateTime.Now;
                payment.TransactionId = tid;
                await db.SaveChangesAsync();

                _logger.LogInformation("UpdatePaymentSucceed completed - PaymentId: {PaymentId}, OrderCode: {OrderCode}, Status changed to Success", 
                    payment.Id, orderCode);
                return true;
            }

            _logger.LogWarning("UpdatePaymentSucceed failed - Payment not found or not in Pending status for OrderCode: {OrderCode}", orderCode);
            return false;
        }

        internal async Task<bool> UpdateReservationStatus(int resId, BookingStatus BookingStatus, BookingStatus? oldBookingStatus = null)
        {
            _logger.LogInformation("UpdateReservationStatus started for ReservationId: {ReservationId}, NewStatus: {NewStatus}, ExpectedOldStatus: {OldStatus}", 
                resId, BookingStatus, oldBookingStatus);

            var reservation = await db.Reservations.FirstOrDefaultAsync(p => p.Id == resId) ?? throw new InvalidDataException("Reservation not found");
            if (oldBookingStatus == null || reservation?.BookingStatus == oldBookingStatus)
            {
                reservation.BookingStatus = BookingStatus;
                reservation.DateFinalized = DateTime.Now;
                await db.SaveChangesAsync();

                _logger.LogInformation("UpdateReservationStatus completed - ReservationId: {ReservationId}, Status changed to: {NewStatus}", 
                    resId, BookingStatus);
                return true;
            }

            _logger.LogWarning("UpdateReservationStatus failed - ReservationId: {ReservationId}, Current status {CurrentStatus} does not match expected {ExpectedStatus}", 
                resId, reservation?.BookingStatus, oldBookingStatus);
            return false;
        }

        internal async Task<bool> UpdateReservationRateStatus(int rateId, BookingStatus BookingStatus, BookingStatus oldBookingStatus)
        {
            _logger.LogDebug("UpdateReservationRateStatus started for RateId: {RateId}, NewStatus: {NewStatus}, ExpectedOldStatus: {OldStatus}", 
                rateId, BookingStatus, oldBookingStatus);

            var rate = await db.ReservationRates.FirstOrDefaultAsync(p => p.Id == rateId);
            if (rate?.BookingStatus == oldBookingStatus)
            {
                rate.BookingStatus = BookingStatus;
                rate.DateFinalized = DateTime.Now;
                await db.SaveChangesAsync();

                _logger.LogDebug("UpdateReservationRateStatus completed - RateId: {RateId}, Status changed to: {NewStatus}", 
                    rateId, BookingStatus);
                return true;
            }

            _logger.LogWarning("UpdateReservationRateStatus failed - RateId: {RateId}, Current status does not match expected {ExpectedStatus}", 
                rateId, oldBookingStatus);
            return false;
        }

        internal async Task<bool> UpdateReservationRateStatusConfirmed(int rateId, BookingStatus BookingStatus, int ProviderResId)
        {
            _logger.LogInformation("UpdateReservationRateStatusConfirmed started for RateId: {RateId}, ProviderResId: {ProviderResId}", 
                rateId, ProviderResId);

            var reservation = await db.ReservationRates.FirstOrDefaultAsync(p => p.Id == rateId);
            if (reservation?.BookingStatus == BookingStatus.Running)
            {
                reservation.BookingStatus = BookingStatus;
                reservation.DateFinalized = DateTime.Now;
                reservation.ProviderResId = ProviderResId;
                await db.SaveChangesAsync();

                _logger.LogInformation("UpdateReservationRateStatusConfirmed completed - RateId: {RateId}, Status: {Status}, ProviderResId: {ProviderResId}", 
                    rateId, BookingStatus, ProviderResId);
                return true;
            }

            _logger.LogWarning("UpdateReservationRateStatusConfirmed failed - RateId: {RateId} not in Running status", rateId);
            return false;
        }

        internal async Task<bool> UpdatePaymentFailed(Payment payment)
        {
            _logger.LogInformation("UpdatePaymentFailed started for PaymentId: {PaymentId}, OrderCode: {OrderCode}", 
                payment?.Id, payment?.OrderCode);

            if (payment?.PaymentStatus == PaymentStatus.Pending)
            {
                payment.PaymentStatus = PaymentStatus.Failed;
                payment.DateFinalized = DateTime.Now;
                await db.SaveChangesAsync();

                _logger.LogInformation("UpdatePaymentFailed completed - PaymentId: {PaymentId}, OrderCode: {OrderCode}, Status changed to Failed", 
                    payment.Id, payment.OrderCode);
                return true;
            }

            _logger.LogWarning("UpdatePaymentFailed: Payment not in Pending status, PaymentId: {PaymentId}", payment?.Id);
            return false;
        }

        public async Task<Coupon?> RetrieveCoupon(string couponCode)
        {
            _logger.LogDebug("RetrieveCoupon started for CouponCode: {CouponCode}", couponCode);

            var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.Expiration > DateTime.UtcNow);

            if (coupon != null)
            {
                _logger.LogInformation("RetrieveCoupon found valid coupon - Code: {CouponCode}, Type: {CouponType}, Percentage: {Percentage}, Amount: {Amount}", 
                    couponCode, coupon.CouponType, coupon.Percentage, coupon.Amount);
            }
            else
            {
                _logger.LogDebug("RetrieveCoupon: No valid coupon found for Code: {CouponCode}", couponCode);
            }

            return coupon;
        }
    }
}