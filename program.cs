using GIS = GHIElectronics.UWP.Shields;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Tsunami.SafetyLine.API.Client.Models;
using Microsoft.Identity.Client;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Security;
using System.Threading;


namespace FEZHATDemo
{
    public sealed partial class MainPage : Page
    {
        private GIS.FEZHAT hat;
        private DispatcherTimer timer;
        private bool next;
        public HttpClient httpClient = new System.Net.Http.HttpClient();
        private int i;
        private string calendarValue;

        public MainPage()
        {
            this.InitializeComponent();

            this.Setup();
        }

        private async void Setup()
        {
            this.hat = await GIS.FEZHAT.CreateAsync();

            this.hat.S1.SetLimits(500, 2400, 0, 180);
            this.hat.S2.SetLimits(500, 2400, 0, 180);

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromMilliseconds(100);

            IPublicClientApplication confidentialClientApplication = PublicClientApplicationBuilder
               .Create("yourazureappid")
               .WithRedirectUri("urn:ietf:wg:oauth:2.0:oob")

               .WithAuthority(new Uri("https://login.microsoftonline.com/azuresecretstring"))
               .WithLogging((level, message, containsPii) =>
               {
                   Debug.WriteLine($"MSAL: {level} {message} ");
               }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
               .Build();

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
            var result = await confidentialClientApplication.AcquireTokenInteractive(scopes).ExecuteAsync();

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            while (true)
            {
                Task.Delay(5000).Wait();
                var myrequest = new
                {
                    schedules = new List<String>()
                    {
                        "user@email.com"
                    },
                    startTime = new DateTimeTimeZone
                    {
                        DateTime = DateTimeOffset.Now.ToString(),
                        TimeZone = "Pacific Standard Time"
                    },
                    endTime = new DateTimeTimeZone
                    {
                        DateTime = DateTimeOffset.Now.AddHours(.5).ToString(),
                        TimeZone = "Pacific Standard Time"
                    },
                    availabilityViewInterval = 15
                };

                string content = JsonConvert.SerializeObject(myrequest);
                HttpResponseMessage contentResult = await httpClient.PostAsync("https://graph.microsoft.com/v1.0/users/you@microsoft.com/calendar/getschedule", new StringContent(content, Encoding.UTF32, "application/json"));

                string mycontent = await contentResult.Content.ReadAsStringAsync();
                JObject jObject = JObject.Parse(mycontent);
                calendarValue = (string)jObject.SelectToken("value[0].scheduleItems[0].status");

                if (this.calendarValue == "busy")
                {
                    this.hat.D2.Color = this.next ? GIS.FEZHAT.Color.Red : GIS.FEZHAT.Color.Yellow;
                }
                else
                {
                    this.hat.D2.Color = this.next ? GIS.FEZHAT.Color.Green : GIS.FEZHAT.Color.Blue;
                }
            }
        }   
    }
}
