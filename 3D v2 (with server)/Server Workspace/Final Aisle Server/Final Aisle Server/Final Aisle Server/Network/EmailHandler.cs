using System;
using System.Net;
using System.Net.Mail;
using System.Text;

/**
 * Responsible for sending verification emails to clients
 */
namespace Final_Aisle_Server.Network
{
    public static class EmailHandler
    {
        /**
         * Returns true if email is successfully delivered
         */
        public static bool SendVerificationMail(string email, string username, string verifyCode)
        {
            try
            {
                SmtpClient client = new SmtpClient("smtp.gmail.com");
                client.UseDefaultCredentials = false;

                NetworkCredential basicAuthenticationInfo = new NetworkCredential("finalaisle@gmail.com", "QQpsT_T;42397173Rx98");
                client.Credentials = basicAuthenticationInfo;
                client.Port = 587;
                client.EnableSsl = true;

                MailAddress from = new MailAddress("finalaisle@gmail.com", "Final Aisle");
                MailAddress to = new MailAddress(email, username);
                MailMessage mail = new MailMessage(from, to);

                mail.Subject = "Verify Email Address";
                mail.SubjectEncoding = Encoding.UTF8;

                mail.Body = "<body leftmargin=\"0\" marginwidth=\"0\" topmargin=\"0\" marginheight=\"0\" offset=\"0\"><table width=\"800\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" valign=\"top\" align=\"center\"><tr><td width=\"100%\"><img src=\"https://i.imgur.com/tiPyRa7.png\" width=\"800\"/></td></tr><tr><td width=\"80%\"><center><p style=\"font-family:Trebuchet MS;color:#000000;font-size:17px;margin-top:20px\">Hi <b>"
                    + username + "</b>! Your account setup is almost complete. All that's left is email verification– we just want to make sure it's really you! Here is your six-digit alphanumeric confirmation code.</p><h1 style=\"font-family:Arial Black;font-size:32px;color:#111111\">"
                    + verifyCode + "</h1><p style=\"font-family:Trebuchet MS;color:#000000;font-size:17px\">Thank you for installing <b>Final Aisle</b>! See you on the isle :)</p></h1></center></td></tr></table></body>";
                mail.BodyEncoding = Encoding.UTF8;
                mail.IsBodyHtml = true;
                ConsoleLog.WriteSmallIO("Sending a verification email to " + email + "...");

                client.Send(mail);
                return true;
            }
            catch (SmtpException ex)
            {
                ConsoleLog.WriteSmallError("Error occurred while attempting to deliver a verification email to " + email);
                Console.WriteLine(ex.Message);
                return false;
            }
            catch (FormatException)
            {
                ConsoleLog.WriteSmallIO(email + " is not a valid email address!");
                return false;
            }
        }
    }
}
