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
                    HotelName = res.HotelData.Name,
                    TotalAmount = res.TotalPrice,
                    TotalRooms = res.Rooms.Count,
                    Party = party,
                    Customer = new Customer(pars.CustomerInfo?.Name, pars.CustomerInfo?.LastName, pars.CustomerInfo?.Phone, "GR", pars.CustomerInfo?.Email),
                    Payments = new List<Payment>
                {
                    new() {
                        Amount = res.TotalPrice,
                        OrderCode = orderCode,
                        PaymentProvider=Models.PaymentProvider.Viva,
                        PaymentStatus=Models.PaymentStatus.Pending
                    }
                },
                    Rates = res.Rooms.Select(r => new ReservationRate
                    {
                        HotelCode = pars.HotelId,
                        Price = r.TotalPrice,
                        Provider = Models.Provider.WebHotelier,
                        RateId = r.RateId,
                        Quantity = r.SelectedQuantity
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
                .Include(r => r.Rates)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync();
        }

        internal async Task<Reservation?> GetReservationBasicDataByPaymentCode(string orderCode)
        {
            return await db.Reservations
                .Where(r => r.Payments.Any(p => p.OrderCode == orderCode))
                .Select(r => new Reservation
                {
                    Id = r.Id,
                    CheckIn = r.CheckIn,
                    CheckOut = r.CheckOut,
                    HotelName = r.HotelName,
                    TotalAmount = r.TotalAmount
                })
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