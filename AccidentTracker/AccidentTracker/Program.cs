using DAL;
using log4net;
using log4net.Config;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio;

[assembly: XmlConfigurator]
namespace AccidentTracker
{

    enum ParsingState
    {
        ShouldWeScrape,
        YesScrape,
        HomePage,
        AcceptTerms,
        EnterSearchCriteria,
        ParseResults,
        ParseMoreResults,
        NoDataFound,
        DataFound,
        FoundNewAccidents,
        FetchedExistingAccidents,
        SavedNewAccidents,
        NotifiedNewAccidents,
        NoNewAccidents
    };
    class Program
    {
  
        private static ParsingState parsingState = ParsingState.ShouldWeScrape;
        private static DateTime startdate;
        private static DateTime enddate;
        private static DateTime LastRunDateTime ;
        private static DateTime? LastAccidentNotificationDateTime;
        private static int iProgramID = 0;
        private static AccidentProgram program = new AccidentProgram();
        enum AlertType
        {
            NewMessage,
            Error
        };

        private static ILog Log = LogManager.GetLogger(typeof(AccidentTracker.Program));
        private static List<Data> newAccidents = new List<Data>();
        static void Main(string[] args)
        {
            int iDays = 0;
            bool bScrapeData = false;
            string error = string.Empty;
            AgencyScrapercs scraper = new AgencyScrapercs();
            List<Data> datascraped = new List<Data>();
            

            Log.InfoFormat("Running the program");

            if (args.Length < 1)
            {
                Log.ErrorFormat("Must specify at least the program id");
                Environment.Exit(1);
            }
            iProgramID = int.Parse(args[0]);
            Log.InfoFormat("Program ID is " + iProgramID);

            program = FetchProgramDetails(iProgramID);
            Log.InfoFormat("Program Name found is ->" + program.ProgramName);

            if (args.Length > 1)
            {
                iDays = int.Parse(args[1]);
                Log.InfoFormat("iDays are " + iDays);
            }
            try
            {

                startdate = DateTime.Now.AddDays(-iDays);
                LastRunDateTime = DateTime.Now.AddDays(-iDays);
                enddate = DateTime.Now;

                TimeSpan ts = new TimeSpan(00, 00, 0);
                bScrapeData = ShouldWeScrapeData();

                if (UseLastRunDate())
                    startdate = new DateTime(LastRunDateTime.Year, LastRunDateTime.Month, LastRunDateTime.Day);
                else
                    startdate = new DateTime(startdate.Year, startdate.Month, startdate.Day);
                enddate = new DateTime(enddate.Year, enddate .Month, enddate .Day);

                Log.InfoFormat("startdate " + startdate);
                Log.InfoFormat("enddate " + enddate);

                Log.InfoFormat("scraping data->" + bScrapeData);
                
                if (bScrapeData)
                {
                    parsingState = ParsingState.YesScrape;
                    datascraped = scraper.ScrapeAgencyData(program, out parsingState, startdate, enddate);

                    // fetch data from table for the start date and end date and store in dictionary with key

                    var accidents = GetExistingAccidents();
                    parsingState = ParsingState.FetchedExistingAccidents;

                    
                    // search against data fetched, if does not exists then put it into new list and write into table and send email
                    foreach (var d in datascraped)
                    {
                        if (!accidents.ContainsKey(d.Element1))
                        {
                            newAccidents.Add(d);
                        }

                    }
                    if (newAccidents.Count > 0)
                    {
                        parsingState = ParsingState.FoundNewAccidents;
                        UpdateAccidents(newAccidents);
                        parsingState = ParsingState.SavedNewAccidents;

                        Notify(newAccidents);
                        

                    }
                    else
                    {
                        parsingState = ParsingState.NoNewAccidents;

                    }


                }
            }
            catch (Exception ex)
            {

                Log.ErrorFormat("Exception occured " + ex.ToString());
                Log.ErrorFormat("Inner Exception " + ex.InnerException.ToString());
                
                error = ex.ToString();
                Console.WriteLine(Environment.NewLine + "Exception: " + ex.Message);
                Alert(ex.ToString(),ex.ToString(), AlertType.Error);
            }


            try
            {


                
                if (bScrapeData)
                    UpdateJobStatus(error, newAccidents);
            }
            catch(Exception ex)
            {
                Log.ErrorFormat("Exception occured " + ex.ToString());
                Log.ErrorFormat("Inner Exception " + ex.InnerException.ToString());
                Alert(ex.ToString(),ex.ToString(), AlertType.Error);
                
            }


            Log.Info("Program ended");
            
                

           // Console.ReadLine();
            //Console.Write(Environment.NewLine + "Press any key to continue.");
            //Console.ReadKey();
        }

        private static AccidentProgram FetchProgramDetails(int iProgramID)
        {
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {
                var program =
                                    (from c in context.AccidentProgram
                                     where c.Id == iProgramID
                                     select c).First();

                return program;
            }
        }

        private static void UpdateAccidents(List<Data> newAccidents)
        {
            
            List<Accident> acc = new List<Accident>();
            foreach(var newacc in newAccidents)
            {
                acc.Add(new Accident { AccidentID = newacc.Element1, AccidentDate = DateTime.Parse(newacc.Element2), Name = newacc.Element3, InsertDateTime = DateTime.Now, ProgramID = iProgramID });
            }
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {

                context.Accident.AddRange(acc);

                context.SaveChanges();

            }
        }

        private static void Notify(List<Data> data)
        {
            if (data.Count > 0)
            {
                string textmessage = FormatTextMessage(data);
                string emailmessage = FormatEmailMessage(data);


                Alert(textmessage, emailmessage, AlertType.NewMessage);
                parsingState = ParsingState.NotifiedNewAccidents;
            }
        }

        private static string FormatEmailMessage(List<Data> data)
        {
            StringBuilder message = new StringBuilder();
            message.Append(String.Format("{0} <br/><br/>", program.URL));
            message.Append(String.Format("There are {0} new report(s) from the {1} website.<br/><br/>", data.Count, program.ProgramDescription));

            string[] programelements = program.ProgramDataElementDescriptions.Split('~');
            
            foreach (var d in data)
            {
                message.Append(programelements[0] + d.Element1 );
                message.Append("<br/>");
                message.Append(programelements[1] + d.Element2);
                message.Append("<br/>");
                message.Append(programelements[2] + d.Element3);
                message.Append("<br/>");
                message.Append("<br/>");
                
            }

            return message.ToString();
        }

        private static string FormatTextMessage(List<Data> data)
        {
            StringBuilder message = new StringBuilder();
            message.Append(String.Format("\n\n{0} new accidents found for {1} \n", data.Count, program.ProgramDescription));
            foreach (var d in data)
            {
                message.Append(d.Element1 + "-" + d.Element2 + "-" + d.Element3);
                message.Append("\n");
            }

            return message.ToString();
        }

        private static List<AccidentTracker.Subscribers> GetSubscribers(int programID)
        {
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {
                var subscribers = (from c in context.Subscriber
                                 join d in context.Subscription
                                 on c.Id equals d.SubscriberID
                                 where 
                                 d.ProgramId == programID && 
                                  (c.NotifyEmail  == true  ||  c.NotifySMS == true)
                                 select new AccidentTracker.Subscribers { ID = c.Id ,  FirstName = c.FirstName, LastName= c.LastName, Email= c.EmailAddress, Phone= c.Phone, NotifyEmail= c.NotifyEmail, NotifySMS = c.NotifySMS}) .ToList();

                return subscribers;
            }
        
                
        }



        private static void Alert(string textmsg, string emailmessage , AlertType type)
        {
            
            var accountSid = GetTwilioSID(); // Your Account SID from www.twilio.com/console
            var authToken = GetTwilioToken();  // Your Auth Token from www.twilio.com/console
            var FromPhone = GetTwilioFromPhone();



            var twilio = new TwilioRestClient(accountSid, authToken);

            var clients = GetSubscribers(iProgramID);

            LastAccidentNotificationDateTime = GetLastRunDateAccidentsNotified();
            if (LastAccidentNotificationDateTime == null)
                LastAccidentNotificationDateTime = LastRunDateTime;

            textmsg = GetMessage(textmsg, type);

            foreach (var client in clients)
            {

                try
                {
                    Log.InfoFormat("Starting Notification for clientid->" + client.ID);
                    string clientphone = GetNotificationPhoneNumber(client.Phone, type);

                    if (client.NotifyEmail)
                    {
                         SendEmail(emailmessage, client.Email).Wait();
                        
                        
                    }

                    if (client.NotifySMS)
                    {

                        if (textmsg.Length > 1500)
                        {
                            Log.ErrorFormat("truncating text message-> " + textmsg);
                            textmsg = textmsg.Substring(0, 1500);
                        }
                        
                        var message = twilio.SendMessage(
                            FromPhone, // From (Replace with your Twilio number)
                            client.Phone, // To (Replace with your phone number)
                            textmsg
                            );

                        if (message.RestException != null)
                        {
                            var error = message.RestException.Message;
                            Log.InfoFormat("error in messaging->" + error);

                            //throw new ApplicationException(error);
                        }

                    }

                    if (type == AlertType.NewMessage)
                    {

                        AuditAlert(textmsg, client.ID);
                    }

                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Exception occured " + ex.ToString() + " while notifying clientid->" + client.ID);
                }


            }

        }



        private static async Task SendEmail(string message, string clientemail)
        {
            String apiKey = GetSendGridKey(); //Environment.GetEnvironmentVariable("SENDGRID_APIKEY", EnvironmentVariableTarget.User);
            dynamic sg = new SendGrid.SendGridAPIClient(apiKey, "https://api.sendgrid.com");
            var fromEmail = GetAdminEmail();
            Email from = new Email(fromEmail);
            String subject = "I'm replacing the subject tag";
            Email to = new Email(clientemail);
            Content content = new Content("text/html", message);
            Mail mail = new Mail(from, subject, to, content);
            //Mail mail = new Mail();
            //mail.From = from;
            //mail.ReplyTo = to;
            //mail.AddContent(content);

            mail.TemplateId = program.EmailTemplateID;
            mail.Personalization[0].AddSubstitution("-accidentcount-", newAccidents.Count.ToString());
            //mail.Personalization[0].AddSubstitution("-city-", "Denver");

            dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Body.ReadAsStringAsync().Result);
            Console.WriteLine(response.Headers.ToString());

            
            

            

        }

        private static string GetNotificationPhoneNumber(string p, AlertType type)
        {
            if (type == AlertType.Error)
            {
                return GetAdminPhone();
            }
            else
            {
                return "+1" + p;
            }
        }

        private static string GetMessage(string msg, AlertType type)
        {
            if (type == AlertType.Error)
            {
                msg = String.Format("Exception occured->{0} at {1}", msg, DateTime.Now.ToString());
            }
            else
            {
                msg = String.Format("\n New Accident Reported  since {1}->{0}", msg, LastAccidentNotificationDateTime.Value.ToString());
            }
            return msg;
        }


        private static void AuditAlert(string message, long subscriberid)
        {
            
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {

                var alert= new AlertAudit{  ProgramID = iProgramID, message = message, SubscriberID = subscriberid, InsertDateTime= DateTime.Now };

                context.AlertAudit.Add(alert);

                context.SaveChanges();

            }

        }


        private static void UpdateJobStatus(string errormessage,  List<Data> data)
        {
            string LastRunStatus  = "S";
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {
                
                if (errormessage.Trim().Length > 0)
                {
                    LastRunStatus = "E";
                }
                var jobstatus = new JobStatus { ProgramID = iProgramID,  LastRunErrorState = parsingState.ToString(),  LastRunDateTime = DateTime.Now, LastRunError = errormessage, LastRunStatus = LastRunStatus , NewAccidentCount = data.Count};

                context.JobStatus.Add(jobstatus);

                context.SaveChanges();
                
            }
            
        }

        

        private static bool UseLastRunDate()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["UselastRunDateAsStartDate"] ?? "N";
            return bool.Parse(result);
        }


        private static string  GetAdminPhone()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["adminphone"] ?? "";
            return result;
        }
        private static string  GetTwilioSID()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["twilio_sid"] ?? "";
            return result;
        }

        private static string GetTwilioToken()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["twilio_token"] ?? "";
            return result;
        }

        private static string GetSendGridKey()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["sendgrid_key"] ?? "";
            return result;
        }
        private static string GetTwilioFromPhone()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["twilio_fromphone"] ?? "";
            return result;
        }


        private static string GetAdminEmail()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["adminemail"] ?? "";
            return result;
        }

        private static int GetInterval()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["jobinterval_error"] ?? "30";
            return Int32.Parse(result);
        }
        private static bool ShouldWeScrapeData()
        {
            // always scrape by default
            bool bScrapeData = true;
            
            
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {
                var jobstatus = context.JobStatus.Where(a => a.ProgramID == iProgramID).OrderByDescending(a => a.LastRunDateTime).Take(1).FirstOrDefault();
                if (jobstatus == null)
                    return bScrapeData;
                else
                {
                    // if error then wait atleast 30 min
                    if (jobstatus.LastRunStatus.Equals("E"))
                    {
                        TimeSpan span = jobstatus.LastRunDateTime - DateTime.Now;
                        if (span.Minutes < GetInterval())
                        {
                            bScrapeData = false;
                        }
                    }
                    else
                    {
                        LastRunDateTime = jobstatus.LastRunDateTime;
                    }
                }

            }
            return bScrapeData;
        }
        private static DateTime? GetLastRunDateAccidentsNotified()
        {
            // always scrape by default

            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {
                var jobstatus =
                                    (from c in context.JobStatus
                                        where c.ProgramID == iProgramID
                                        && c.LastRunErrorState.Equals("NotifiedNewAccidents")
                                        select c).OrderByDescending(a=>a.LastRunDateTime).FirstOrDefault();

//context.JobStatus.Take(1).Where(a => a.ProgramID == ProgramID)
  //                  && a.LastRunErrorState.Equals( "NotifiedNewAccidents")).OrderByDescending(a => a.LastRunDateTime).FirstOrDefault();
                if (jobstatus == null)
                    return null;
                else
                {
                    return jobstatus.LastRunDateTime;
                }

            }
        }


        private static Dictionary<string, Accident> GetExistingAccidents()
        {
            using (ReportTrackerEntities context = new ReportTrackerEntities())
            {
                var accidents = (from c in context.Accident
                                 where c.AccidentDate >= startdate && c.AccidentDate <= enddate
                                 select c).ToList();

                Dictionary<string, Accident> accidentsDict = new Dictionary<string, Accident>();
                foreach (var accident in accidents)
                {
                    accidentsDict.Add(accident.AccidentID, accident);
                }
                return accidentsDict;
            }
            
        }

    }
}
