using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Twilio;

namespace ReportTrackerNotification
{
    class Program
    {
        static void Main(string[] args)
        {

            var accountSid = "ACe32ce832cac98a89e36a9939ae7ce7c1"; // Your Account SID from www.twilio.com/console
            var authToken = "5e37609b8236468c2c585eb3ebca8eb4";  // Your Auth Token from www.twilio.com/console

            var twilio = new TwilioRestClient(accountSid, authToken);
            var message = twilio.SendMessage(
                "+15132990287", // From (Replace with your Twilio number)
                "+15135502505", // To (Replace with your phone number)
                "Hello from C#"
                );

            if (message.RestException != null)
            {
                var error = message.RestException.Message;
                Console.WriteLine(error);
                Console.Write("Press any key to continue.");
                Console.ReadKey();
            }

        }
    }
}
