using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StarAlliance_AirlineParser
{
    class Program
    {
        static void Main(string[] args)
        {
            // Getting Json for cities
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko");
                client.Headers.Add("Accept", "application/json, text/plain, */*");
                client.Headers.Add("Accept-Language", "en-gb,en;q=0.5");
                client.Headers.Add("Referer", "https://www.ada-aero.com/");
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json;charset=utf-8");
                client.Encoding = Encoding.UTF8;
                string citiesjson = client.DownloadString("https://www.ada-aero.com/app/api/ServiceKiu");
                dynamic dynJson = JsonConvert.DeserializeObject(JToken.Parse(citiesjson).ToString());
                foreach (var from in dynJson.Citys)
                {
                    Console.WriteLine("Parsing flights from: {0} - {1}", from.CityName, from.IataCode);
                    foreach (var to in from.Locations)
                    {
                        Console.WriteLine("Getting flight: {0} - {1}", from.CityName, to.CityName);
                        using (WebClient clientFlightCheck = new WebClient())
                        {
                            clientFlightCheck.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko");
                            clientFlightCheck.Headers.Add("Accept", "application/json, text/plain, */*");
                            clientFlightCheck.Headers.Add("Accept-Language", "en-gb,en;q=0.5");
                            //client.Headers.Add("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
                            clientFlightCheck.Headers.Add(HttpRequestHeader.ContentType, "application/json;charset=utf-8");
                            clientFlightCheck.Encoding = Encoding.UTF8;
                            clientFlightCheck.Headers.Add("Referer", "https://www.ada-aero.com/");

                            var payloadFlightCheck = new FlightCheck
                            {
                                DepartureDateTime = "28/12/2016",
                                OriginLocation = from.IataCode,
                                DestinationLocation = to.IataCode,
                                PassengerTypeQuantityADT = "1",
                                PassengerTypeQuantityCNN = "0"
                            };

                            string response = clientFlightCheck.UploadString(new Uri("https://www.ada-aero.com/app/api/AirAvailRQ"), "POST", JsonConvert.SerializeObject(payloadFlightCheck));
                            //Console.WriteLine(response);
                            dynamic dynJsonResult = JsonConvert.DeserializeObject(response);
                            foreach (var flight in dynJsonResult)
                            {
                                if (flight.TipoVuelo == "Directo")
                                {
                                    // Only Direct flights
                                    Console.WriteLine("{0} - {1}", flight.NumeroVuelo, flight.Ruta);
                                }
                            }
                        }
                    }
                }
            }





        }

        public class FlightCheck
        {
            [JsonProperty("DepartureDateTime")]
            public string DepartureDateTime { get; set; }

            [JsonProperty("OriginLocation")]
            public string OriginLocation { get; set; }

            [JsonProperty("DestinationLocation")]
            public string DestinationLocation { get; set; }

            [JsonProperty("PassengerTypeQuantityADT")]
            public string PassengerTypeQuantityADT { get; set; }

            [JsonProperty("PassengerTypeQuantityCNN")]
            public string PassengerTypeQuantityCNN { get; set; }
        }


    }
}
