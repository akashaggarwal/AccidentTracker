using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Specialized;
using System.Web;
using System.Collections;
using System.Text.RegularExpressions;
using ScrapySharp.Network;
using ScrapySharp.Extensions;
using HtmlAgilityPack;
using System.Configuration;
using DAL;
namespace AccidentTracker
{
    
        class Data
        {
            public string Element1 { get; set; }
            public string Element2 { get; set; }
            public string Element3 { get; set; }
        }
    class AgencyScrapercs
    {
        #region Constants

        //private string urlAgencyPage = "http://www.cityofmiddletown.org/police/reports/";
        private string urlAgencyPage = "";
        private const int pageTimeout = 120000;
        
                
        #endregion Constants

        #region Fields

        private CookieContainer cookies = new CookieContainer();
        private string lastSearchPageText = string.Empty;
        private List<string> brandButtons;
        static List<Data> data = new List<Data>();
        private static AccidentProgram program = new AccidentProgram();
        #endregion Fields
                
        #region Public Methods

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// Scrape the agency information from the Agency website.
        /// </summary>
        //-----------------------------------------------------------------------------------------
        public List<Data> ScrapeAgencyData(AccidentProgram p, out ParsingState parsingState, DateTime startDate, DateTime enddate)
        {

            urlAgencyPage = p.URL;

            Guid g;
            // Create and display the value of two GUIDs.
            g = Guid.NewGuid();
            List<Data> sorteddata = new List<Data>();
            program = p;
            //-----------------------------------------------------------------
            // Load the home page of the website
            //-----------------------------------------------------------------
            lastSearchPageText = GetHomePage();
            WriteDebugInfo(lastSearchPageText, 0, g.ToString());
            parsingState = ParsingState.HomePage;
            //-----------------------------------------------------------------
            // Hit the search button to bring back all of the agencies
            //-----------------------------------------------------------------
            lastSearchPageText = PostToAgencyPage("ctl00$ctl00$Master_Page$Page_Content_Right$lb2", "Access Public Reports",0, startDate, enddate);
            WriteDebugInfo(lastSearchPageText, 1, g.ToString());
            parsingState = ParsingState.AcceptTerms;

            lastSearchPageText = PostToAgencyPage("", "Step 1: Select search type:", 1, startDate, enddate);
            WriteDebugInfo(lastSearchPageText, 2, g.ToString());
            parsingState = ParsingState.EnterSearchCriteria;

            lastSearchPageText = PostToAgencyPage("", "updatePanel|ctl00_ctl00_Master_Page_Page_Content_Right_upd|", 2, startDate, enddate);
            WriteDebugInfo(lastSearchPageText, 3, g.ToString());
            parsingState = ParsingState.ParseResults;

            if (lastSearchPageText.Contains("There is no data available"))
            {
                Console.WriteLine("no alerts");
                parsingState = ParsingState.NoDataFound;
            }

            else
            {

                


                 GetData(lastSearchPageText);
                 parsingState = ParsingState.DataFound;
                // check how many pages

                int maxpages  = Checknumberofpages(lastSearchPageText);
                // more than one page detected
                if (maxpages  > 1)
                {
                    // start from Page 2 and keep going until max pages
                    int currentPageNumber = 2;
                    do
                    {
                        lastSearchPageText = PostToAgencyPage("ctl00$ctl00$Master_Page$Page_Content_Right$gv", "updatePanel|ctl00_ctl00_Master_Page_Page_Content_Right_upd|", 3,  startDate, enddate,currentPageNumber);
                        WriteDebugInfo(lastSearchPageText, 3 + currentPageNumber, g.ToString());
                        currentPageNumber = currentPageNumber + 1;
                        GetData(lastSearchPageText);
                        parsingState = ParsingState.ParseMoreResults;

                    } while (currentPageNumber < maxpages);





                }

                sorteddata = data.OrderBy(a => a.Element2.ToDate()).ThenBy(a => a.Element1).ToList();

                //using (StreamWriter sw = new StreamWriter(@"c:\output.txt"))
                //{
                //    foreach (var d in sorteddata)
                //    {
                //        string temp = String.Format("{0} - {1} - {2}", d.Element1, d.Element2, d.Element3);
                //        sw.WriteLine(temp);
                    
                //    }
                
                //}

            }


            
            return sorteddata;            

        }

        private static void WriteDebugInfo(string datascraped, int stage, string guid)
        {
            var appSettings = ConfigurationManager.AppSettings;
            string result = appSettings["debug"] ?? "off";
            if (result == "on")
            {
                string filepath = appSettings["debugfilepath"] ?? "c:\temp";
                string filename = filepath + @"\data_" + stage + "_" + guid + ".txt";
               using ( StreamWriter sw = new StreamWriter(filename))
               {
                   sw.WriteLine(datascraped);
               }

            }
        }

        private static int Checknumberofpages(string lastSearchPageText)
        {
            List<Data> data = new List<Data>();
            int numberofpages = 1;
            var html = new HtmlDocument();
            html.LoadHtml(lastSearchPageText);
            var root = html.DocumentNode;

            try
            {
                var tablerowNodes = root.Descendants()
                    .Where(n => n.GetAttributeValue("id", "").Equals("ctl00_ctl00_Master_Page_Page_Content_Right_gv"))
                    .Single()
                    .Descendants().Where(n => n.GetAttributeValue("class", "").Equals("gvPager")).Single().Descendants("a");



                foreach (HtmlNode row in tablerowNodes)
                {

                    Console.WriteLine(row.InnerHtml);
                    numberofpages += 1;
                }
            }
            catch(Exception ex )
            {

            }
            return numberofpages;
        }
       
         private static  void GetData(string lastSearchPageText)
        {
            
            var html = new HtmlDocument();
            html.LoadHtml(lastSearchPageText);
            var root = html.DocumentNode;
            var tablerowNodes = root.Descendants()
                .Where(n => n.GetAttributeValue("id", "").Equals("ctl00_ctl00_Master_Page_Page_Content_Right_gv"))
                .Single()
                .Descendants("tr")
                .Where(n => n.GetAttributeValue("class", "").EndsWith("style"));



            foreach (HtmlNode row in tablerowNodes)
            {
                
                //Console.WriteLine(row.InnerHtml);
                int columnindex = 0;
                Data temp = new Data();

                foreach (HtmlNode column in row.Descendants("td"))
                {
                   // Console.WriteLine(column.InnerHtml);
                    columnindex = columnindex + 1;
                    switch (columnindex)
                    {
                        case 1:
                            {
                                temp.Element1 = column.InnerHtml;
                                break;
                            }
                        case 2:
                            {
                                temp.Element2 = column.InnerHtml;
                                break;
                            }
                        case 3:
                            {
                                temp.Element3 = column.InnerHtml;
                                break;
                            }
                        case 4:
                            {
                                data.Add(temp);
                                break;
                            }


                    }

                }
            }

            
        }
        #endregion Public Methods

        #region Private Methods

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// You must start here by loading the Agency page. This will initialize our session.
        /// </summary>
        //-----------------------------------------------------------------------------------------
        private string GetHomePage()
        {
            string pageText = string.Empty;

            // Create custom SSL handler
            ServicePointManager.ServerCertificateValidationCallback +=
                delegate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    bool validationResult = true;
                    return validationResult;
                };

            HttpWebRequest webRequest = CreateWebRequest("GET", program.URL);

            //Console.WriteLine("Getting " + urlAgencyPage);

            using (StreamReader responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream()))
            {
                pageText = responseReader.ReadToEnd();
            }

            // Close the response stream.
            webRequest.GetResponse().Close();

            // Check to make sure we got the correct page
            if (!pageText.Contains("Welcome to the City of Middletown's Police Reporting System"))
            {
                throw new ApplicationException("Received Invalid Page. Expected SEARCH CRITERIA page.");
            }

            return pageText;
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// Post to the Agency page. The eventTarget determines what the site does. Options include
        /// searching for Agencies, going to the view brands page or selecting the next page.
        /// </summary>
        //-----------------------------------------------------------------------------------------
        private string PostToAgencyPage(string eventTarget, string validateText, int stage,  DateTime startdate, DateTime enddate , int PageNumber = 0)
        {
            string pageText = string.Empty;
            string captcha = string.Empty;
            System.Text.Encoding enc;
            StringBuilder sbPostData = new StringBuilder();

            // extract the viewstate value and build out POST data
            string viewState = ExtractInputValue("__VIEWSTATE", lastSearchPageText);
            string eventValidation = ExtractInputValue("__EVENTVALIDATION", lastSearchPageText);
            string viewStategenerator = ExtractInputValue("__VIEWSTATEGENERATOR", lastSearchPageText);
            //StringDictionary postData = new StringDictionary();

            Dictionary<string, string> postData = new Dictionary<string, string>();

            postData.Add("ctl00_ctl00_ToolkitScriptManager1_HiddenField", ";;AjaxControlToolkit, Version=3.5.40412.0, Culture=neutral, PublicKeyToken=28f01b0e84b6d53e:en-US:1547e793-5b7e-48fe-8490-03a375b13a33:475a4ef5:effe2a26:1d3ed089:5546a2b:497ef277:a43b07eb:751cdd15:dfad98a5:3cf12cf1");
            postData.Add("__EVENTTARGET", eventTarget);
            

            if (stage == 3)
            {
                // partial page is returned so need to get viewstate etc from that partial page because of AJAX request the data is not in standard form like input type=hidden
                // but it is sent as below
                //|0|hiddenField|__EVENTTARGET||0|hiddenField|__EVENTARGUMENT||3008|hiddenField|__VIEWSTATE|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|8|hiddenField|__VIEWSTATEGENERATOR|AC24AE23|288|hiddenField|__EVENTVALIDATION|/wEWIAKgzpj2AQLPq6TkDALpj5vXBwLpj5/XBwLpj5PXBwLpj5fXBwLpj4vXBwLpj4/XBwLpj4PXBwLpj4fXBwLpj/vXBwLpj//XBwLPyNfcCgK0vvVBApGVk6oFAv6MsZ8LAtvjrIABAsDayuo
                viewState = ExtractValue("__VIEWSTATE", lastSearchPageText);
                eventValidation = ExtractValue("__EVENTVALIDATION", lastSearchPageText);
                viewStategenerator = ExtractValue("__VIEWSTATEGENERATOR", lastSearchPageText);
                
                
            }

            postData.Add("__VIEWSTATE", viewState);
            postData.Add("__VIEWSTATEGENERATOR", viewStategenerator);
            postData.Add("__EVENTVALIDATION", eventValidation);

            if (stage == 1)
            {
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$cbAgree", "on");
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$btnAgree", "Access Public Reports");
                //urlAgencyPage  = @"http://www.cityofmiddletown.org/police/reports/default.aspx";

                urlAgencyPage = program.URL + "default.aspx";
            }

            if (stage == 2)
            {


                captcha = ExtractInputValue("LBD_VCID_c_police_reports_accidents_ctl00_ctl00_master_page_page_content_right_samplecaptcha", lastSearchPageText);

                postData.Add("ctl00$ctl00$ToolkitScriptManager1", "ctl00$ctl00$ToolkitScriptManager1|ctl00$ctl00$Master_Page$Page_Content_Right$btnSubmit");
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$viewerID", "");
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$searchTypes", "DATE");
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$txtSearch", startdate.ToShortDateString());
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$txtSearch2", enddate.ToShortDateString());
                postData.Add("__ASYNCPOST", "true");

                postData.Add("LBD_VCID_c_police_reports_accidents_ctl00_ctl00_master_page_page_content_right_samplecaptcha", captcha);
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$TextBoxWatermarkExtender1_ClientState","");
	            postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$tbCaptcha","");
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$btnSubmit", "");	


                //urlAgencyPage = @"http://www.cityofmiddletown.org/police/reports/accidents.aspx";

                urlAgencyPage = program.URL + "accidents.aspx";

            }

            if (stage == 3)
            {


                captcha = ExtractInputValue("LBD_VCID_c_police_reports_accidents_ctl00_ctl00_master_page_page_content_right_samplecaptcha", lastSearchPageText);

                postData.Add("ctl00$ctl00$ToolkitScriptManager1", "ctl00$ctl00$ToolkitScriptManager1|ctl00$ctl00$Master_Page$Page_Content_Right$gv");
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$viewerID", "");
                postData.Add("__ASYNCPOST", "true");

                postData.Add("LBD_VCID_c_police_reports_accidents_ctl00_ctl00_master_page_page_content_right_samplecaptcha", captcha);
                postData.Add("ctl00$ctl00$Master_Page$Page_Content_Right$tbCaptcha", "");
                postData.Add("__EVENTARGUMENT", String.Format("Page${0}", PageNumber));	
            

                //urlAgencyPage = @"http://www.cityofmiddletown.org/police/reports/accidents.aspx";

                urlAgencyPage = program.URL + "accidents.aspx";
            }

            
            //postData.Add("ctl00_MainContent_txtName_text", "");
            //postData.Add("ctl00$MainContent$txtName", "");
            //postData.Add("ctl00_MainContent_txtName_ClientState", "");
            //postData.Add("ctl00$MainContent$ddlPermitClass", "--Select Permit Class--");
            //postData.Add("ctl00_MainContent_ddlPermitClass_ClientState", @"{""logEntries"":[],""value"":""0"",""text"":""--Select Permit Class--"",""enabled"":true}");
            //postData.Add("ctl00_MainContent_txtCity_text", "");
            //postData.Add("ctl00$MainContent$txtCity", "");
            //postData.Add("ctl00_MainContent_txtCity_ClientState", "");
            //postData.Add("ctl00_MainContent_txtZip_text", "#####");
            //postData.Add("ctl00$MainContent$txtZip", "#####");
            //postData.Add("ctl00_MainContent_txtZip_ClientState", "");
            //postData.Add("ctl00$MainContent$ddlCounty", "--Select County--");
            //postData.Add("ctl00_MainContent_ddlCounty_ClientState", @"{""logEntries"":[],""value"":""0"",""text"":""--Select County--"",""enabled"":true}");
            //postData.Add("ctl00_MainContent_btnSearch_ClientState", @"{""text"":""Wait"",""checked"":false,""target"":"""",""navigateUrl"":"""",""commandName"":"""",""commandArgument"":"""",""autoPostBack"":true,""selectedToggleStateIndex"":0,""readOnly"":false}");
            //postData.Add("ctl00_MainContent_btnReset_ClientState", "");
            //postData.Add("ctl00_MainContent_ucMainMenu1_btnMainMenu_ClientState", "");
            //postData.Add("ctl00$MainContent$dgAgency$ctl00$ctl02$ctl01$PageSizeComboBox", "50");
            //postData.Add("ctl00$MainContent$dgAgency$ctl00$ctl03$ctl01$PageSizeComboBox", "10");

            foreach (KeyValuePair<string, string>  entry in postData)
            {
                if (sbPostData.Length > 0) sbPostData.Append('&');

                sbPostData.Append(entry.Key + "=" + HttpUtility.UrlEncode(entry.Value.ToString()));
            }

            HttpWebRequest webRequest = CreateWebRequest("POST", urlAgencyPage, stage);

            //Console.WriteLine("Posting to " + urlAgencyPage);

            // Write the form values into the request message
            using (StreamWriter sw = new StreamWriter(webRequest.GetRequestStream()))
            {
                sw.Write(sbPostData.ToString());
            }

            enc = System.Text.Encoding.GetEncoding("utf-8");
            using (StreamReader sr = new StreamReader(webRequest.GetResponse().GetResponseStream(), enc))
            {
                pageText = sr.ReadToEnd();
            }

            cookies = webRequest.CookieContainer;

            
            // Close the response stream.
            webRequest.GetResponse().Close();

            // Check to make sure we got the correct page
            if (!pageText.Contains(validateText))
            {
                throw new ApplicationException("Received Invalid Page. Expected " + validateText + " page.");
            }

            //Console.WriteLine("Successfully navigated to " + urlAgencyPage);

            return pageText;
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// Use a regular expression to get the names of all of the link buttons on the current
        /// agency page.
        /// </summary>
        //-----------------------------------------------------------------------------------------


        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// Extract the given value from the give html
        /// </summary>
        //-----------------------------------------------------------------------------------------
        private string ExtractInputValue(string valueName, string html)
        {
            string valueDelimiter = "value=\"";

            int position = html.IndexOf(valueName);

            if (position < 0) return string.Empty;

            int valuePosition = html.IndexOf(valueDelimiter, position);

            if (valuePosition < 0) return string.Empty;

            int startPosition = valuePosition + valueDelimiter.Length;
            int endPosition = html.IndexOf("\"", startPosition);

            //make it all pretty for the web
            return html.Substring(startPosition, endPosition - startPosition);
        }
        private string ExtractValue(string valueName, string html)
        {
            

            int position = html.IndexOf(valueName);

            if (position < 0) return string.Empty;

            string subhtml = html.Substring(position);
            string [] data = subhtml.Split('|');
            return data[1];
        }

        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// Create and initialize our HttpWebRequest object with default parameters.
        /// </summary>
        //-----------------------------------------------------------------------------------------
        private HttpWebRequest CreateWebRequest(string method, string url, int stage = 0)
        {
            HttpWebRequest webRequest = WebRequest.Create(url) as HttpWebRequest;
            if (webRequest == null)
                throw new NullReferenceException(string.Format("Unable to create webrequest for Url {0}", url));

            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            //webRequest.Referer = "http://www.cityofmiddletown.org/police/reports/default.aspx";
            webRequest.AllowAutoRedirect = true;
            webRequest.Timeout = pageTimeout;
            webRequest.KeepAlive = true;
            //webRequest.Headers.Add("Upgrade-Insecure-Requests", "1");
            //webRequest.Headers.Add("Origin", "http://www.cityofmiddletown.org");
            webRequest.CookieContainer = cookies;
            webRequest.Method = method;

            if (stage == 2)
            {
                webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
            }
            System.Net.ServicePointManager.Expect100Continue = false;
            if (method.Equals("POST"))
            {
                webRequest.ContentType = "application/x-www-form-urlencoded";
            }

            return webRequest;
        }

        #endregion Private Methods

    }
}
