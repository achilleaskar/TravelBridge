using Microsoft.EntityFrameworkCore;
using TravelBridge.API.Contracts;
using TravelBridge.API.DataBase;
using TravelBridge.API.Models;
using TravelBridge.API.Models.DB;

namespace TravelBridge.API.Repositories
{
    public class ReservationsRepository
    {
        private readonly AppDbContext db;

        public ReservationsRepository(AppDbContext db)
        {
            this.db = db;
        }

        internal async Task CreateTemporaryExternalReservation(CheckoutResponse res, Endpoints.ReservationEndpoints.BookingRequest pars, DateTime parsedCheckin, DateTime parsedCheckOut, Models.ExternalModels.VivaPaymentRequest payment, string orderCode, string party)
        {
            try
            {
                var newRes = new Reservation
                {
                    CheckIn = DateOnly.FromDateTime(parsedCheckin.Date),
                    CheckOut = DateOnly.FromDateTime(parsedCheckOut.Date),
                    HotelCode = pars.HotelId,
                    HotelName = res.HotelData.Name, //TODO: check giati den mpainei
                    TotalAmount = res.TotalPrice,
                    TotalRooms = res.Rooms.Count,
                    Party = party,
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
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        internal async Task<Reservation?> GetFullReservationFromPaymentCode(string orderCode)
        {
            return await db.Reservations
                .Where(r => r.Payments.Any(p => p.OrderCode == orderCode))
                .Include(r => r.Rates).ThenInclude(r => r.SearchParty)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync();
        }

        internal async Task<Reservation?> GetReservationBasicDataByPaymentCode(string orderCode)
        {
            return await db.Reservations
                .Where(r => r.Payments.Any(p => p.OrderCode == orderCode))
                .Include(r => r.Customer)
                .Include(r => r.PartialPayment)
                .Include(r => r.Payments)
                .Include(r => r.Rates).ThenInclude(p => p.SearchParty)
                //.Include(r=>r.Rates)

                //.Select(r => new Reservation
                //{
                //    Id = r.Id,
                //    CheckIn = r.CheckIn,
                //    CheckOut = r.CheckOut,
                //    HotelName = r.HotelName,
                //    HotelCode=r.HotelCode,
                //    TotalAmount = r.TotalAmount,
                //    CheckInTime = r.CheckInTime,
                //    CheckOutTime = r.CheckOutTime,
                //    Rates = =new,
                //    Customer=r.Customer
                //})
                .FirstOrDefaultAsync();
        }

        internal async Task<bool> UpdatePaymentSucceed(string orderCode, string tid)
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderCode == orderCode);
            if (payment?.PaymentStatus == PaymentStatus.Pending)
            {
                payment.PaymentStatus = PaymentStatus.Success;
                payment.DateFinalized = DateTime.Now;
                payment.TransactionId = tid;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        internal async Task<bool> UpdateReservationStatus(int resId, BookingStatus BookingStatus, BookingStatus? oldBookingStatus = null)
        {
            var reservation = await db.Reservations.FirstOrDefaultAsync(p => p.Id == resId) ?? throw new InvalidDataException("Reservation not found");
            if (oldBookingStatus == null || reservation?.BookingStatus == oldBookingStatus)
            {
                reservation.BookingStatus = BookingStatus;
                reservation.DateFinalized = DateTime.Now;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        internal async Task<bool> UpdateReservationRateStatus(int rateId, BookingStatus BookingStatus, BookingStatus oldBookingStatus)
        {
            var rate = await db.ReservationRates.FirstOrDefaultAsync(p => p.Id == rateId);
            if (rate?.BookingStatus == oldBookingStatus)
            {
                rate.BookingStatus = BookingStatus;
                rate.DateFinalized = DateTime.Now;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        internal async Task<bool> UpdateReservationRateStatusConfirmed(int rateId, BookingStatus BookingStatus, int ProviderResId)
        {
            var reservation = await db.ReservationRates.FirstOrDefaultAsync(p => p.Id == rateId);
            if (reservation?.BookingStatus == BookingStatus.Running)
            {
                reservation.BookingStatus = BookingStatus;
                reservation.DateFinalized = DateTime.Now;
                reservation.ProviderResId = ProviderResId;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        internal async Task<bool> UpdatePaymentFailed(Payment payment)
        {
            if (payment?.PaymentStatus == PaymentStatus.Pending)
            {
                payment.PaymentStatus = PaymentStatus.Failed;
                payment.DateFinalized = DateTime.Now;
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}