using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TravelBridge.Core.Interfaces;

namespace TravelBridge.Infrastructure.Integrations.ExternalServices
{
    /// <summary>
    /// SMTP email service implementing IEmailService.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            IEnumerable<string>? cc = null,
            IEnumerable<string>? bcc = null,
            CancellationToken cancellationToken = default)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"]!),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            if (cc != null)
            {
                foreach (var ccEmail in cc)
                    mailMessage.CC.Add(ccEmail);
            }

            if (bcc != null)
            {
                foreach (var bccEmail in bcc)
                    mailMessage.Bcc.Add(bccEmail);
            }

            await SendMailInternalAsync(mailMessage);
        }

        public async Task SendBookingConfirmationAsync(BookingNotification notification, CancellationToken cancellationToken = default)
        {
            var htmlContent = BuildBookingConfirmationHtml(notification);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("bookings@my-diakopes.gr"),
                Subject = "Επιβεβαίωση Κράτησης",
                Body = htmlContent,
                IsBodyHtml = true
            };

            mailMessage.To.Add(notification.CustomerEmail);
            mailMessage.CC.Add("bookings@my-diakopes.gr");
            mailMessage.Bcc.Add("achilleaskaragiannis@outlook.com");

            await SendMailInternalAsync(mailMessage);
        }

        public async Task SendErrorNotificationAsync(string subject, string errorDetails, CancellationToken cancellationToken = default)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress("bookings@my-diakopes.gr"),
                Subject = subject,
                Body = errorDetails,
                IsBodyHtml = true
            };

            mailMessage.To.Add("bookings@my-diakopes.gr");
            mailMessage.To.Add("achilleaskaragiannis@outlook.com");

            try
            {
                await SendMailInternalAsync(mailMessage);
            }
            catch
            {
                // Swallow - error notifications shouldn't fail silently
            }
        }

        /// <summary>
        /// Internal method to send a pre-built MailMessage.
        /// Exposed for backward compatibility with WebHotelierService.
        /// </summary>
        public async Task SendMailAsync(MailMessage mailMessage)
        {
            await SendMailInternalAsync(mailMessage);
        }

        private async Task SendMailInternalAsync(MailMessage mailMessage)
        {
            using var smtpClient = new SmtpClient(_config["Smtp:Host"])
            {
                Port = int.Parse(_config["Smtp:Port"]!),
                Credentials = new NetworkCredential(
                    _config["Smtp:Username"],
                    _config["Smtp:Password"]),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(mailMessage);
        }

        private static string BuildBookingConfirmationHtml(BookingNotification notification)
        {
            var roomDetails = "";
            if (notification.Rooms != null)
            {
                foreach (var room in notification.Rooms)
                {
                    roomDetails += $@"
                        <div>
                            <p><span class='value'>{room.Quantity} x {room.RoomName}</span></p>
                            <p><span class='label'>Διατροφή:</span> <span class='value'>{room.BoardType ?? "Χωρίς διατροφή"}</span></p>
                            <p><span class='label'>Πολιτική ακύρωσης:</span> <span class='value'>{room.CancellationPolicy ?? "Δεν υπάρχει"}</span></p>
                            <p><span class='label'>Κόστος:</span> <span class='value'>{room.Price:F2} €</span></p>
                            <br/>
                        </div>";
                }
            }

            return $@"
                <!DOCTYPE html>
                <html>
                <body>
                    <h1>Επιβεβαίωση Κράτησης</h1>
                    <p>Αγαπητέ/ή {notification.CustomerName},</p>
                    <p>Η κράτησή σας επιβεβαιώθηκε!</p>
                    <h2>{notification.HotelName}</h2>
                    <p><strong>Κωδικοί κράτησης:</strong> {string.Join(", ", notification.ConfirmationCodes ?? Array.Empty<string>())}</p>
                    <p><strong>Check-in:</strong> {notification.CheckIn:dd/MM/yyyy} από τις {notification.CheckInTime}</p>
                    <p><strong>Check-out:</strong> {notification.CheckOut:dd/MM/yyyy} έως τις {notification.CheckOutTime}</p>
                    <p><strong>Διάρκεια:</strong> {notification.Nights} νύχτες</p>
                    <p><strong>Σύνθεση:</strong> {notification.PartyDescription}</p>
                    <h3>Δωμάτια</h3>
                    {roomDetails}
                    <h3>Πληρωμή</h3>
                    <p><strong>Συνολικό ποσό:</strong> {notification.TotalAmount:F2} €</p>
                    <p><strong>Πληρωμένο ποσό:</strong> {notification.PaidAmount:F2} €</p>
                    <p><strong>Υπόλοιπο:</strong> {notification.RemainingAmount:F2} €</p>
                </body>
                </html>";
        }
    }
}
