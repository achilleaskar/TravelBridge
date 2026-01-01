using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using TravelBridge.Core.Interfaces;

namespace TravelBridge.API.Services
{
    public class SmtpEmailSender : IEmailSender, IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        #region IEmailSender Implementation

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"]!),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await SendMailAsync(mailMessage);
        }

        #endregion

        #region IEmailService Implementation

        async Task IEmailService.SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            IEnumerable<string>? cc,
            IEnumerable<string>? bcc,
            CancellationToken cancellationToken)
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

            await SendMailAsync(mailMessage);
        }

        public async Task SendBookingConfirmationAsync(BookingNotification notification, CancellationToken cancellationToken = default)
        {
            // Build the booking confirmation email using the notification details
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

            await SendMailAsync(mailMessage);
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
                await SendMailAsync(mailMessage);
            }
            catch
            {
                // Log but don't throw - error notifications shouldn't fail silently
            }
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

        #endregion

        internal async Task SendMailAsync(MailMessage mailMessage)
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
    }
}
