using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace ofis_ici_yemek_secim_sistemi.Services
{

    public static class EmailService
    {
  
        private static string GetSetting(string key)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Web.config > appSettings içinde '{key}' ayarı bulunamadı veya boş. SMTP ayarlarını Web.config dosyasında doldurun.");
            return value;
        }

        public static void SendPasswordResetEmail(string toEmail, string toName, string resetLink)
        {
            string host = GetSetting("SmtpHost");
            int port = int.Parse(GetSetting("SmtpPort"));
            string user = GetSetting("SmtpUser");
            string pass = GetSetting("SmtpPass");
            bool enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"] ?? "true");
            string fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"];
            string fromName = ConfigurationManager.AppSettings["SmtpFromName"];
            if (string.IsNullOrWhiteSpace(fromEmail)) fromEmail = user;
            if (string.IsNullOrWhiteSpace(fromName)) fromName = "Ofis Yemek Seçim Sistemi";

            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = enableSsl;
                client.Credentials = new NetworkCredential(user, pass);

                string subject = "Şifre Sıfırlama Talebiniz";
                string body = $@"
                    <div style='font-family:Segoe UI,Tahoma,Verdana,sans-serif;max-width:480px;margin:0 auto;'>
                        <h2 style='color:#1e293b;'>Merhaba {WebUtilityHtmlEncode(toName)},</h2>
                        <p style='color:#475569;font-size:0.95rem;line-height:1.6;'>
                            Ofis Yemek Seçim Sistemi hesabınız için bir şifre sıfırlama talebi aldık.
                            Şifrenizi sıfırlamak için aşağıdaki butona tıklayın. Bu bağlantı
                            <strong>30 dakika</strong> boyunca geçerlidir.
                        </p>
                        <p style='text-align:center;margin:2rem 0;'>
                            <a href='{resetLink}' style='background:#3b82f6;color:white;padding:0.75rem 1.5rem;border-radius:8px;text-decoration:none;font-weight:600;display:inline-block;'>
                                Şifremi Sıfırla
                            </a>
                        </p>
                        <p style='color:#94a3b8;font-size:0.8rem;line-height:1.5;'>
                            Eğer bu talebi siz oluşturmadıysanız, bu e-postayı görmezden gelebilirsiniz;
                            hesabınızda herhangi bir değişiklik yapılmayacaktır.
                        </p>
                    </div>";

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = true;

                    client.Send(message);
                }
            }
        }

        private static string WebUtilityHtmlEncode(string text)
        {
            return System.Net.WebUtility.HtmlEncode(text ?? "");
        }
    }
}
