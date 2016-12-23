using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.IO.Compression;
using System.Configuration;
using System.Linq;

namespace StarAlliance_AirlineParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            List<CIFLight> CIFLights = new List<CIFLight> { };
            CultureInfo ci = new CultureInfo("en-US");
            string APIPathAirport = "airport/iata/";
            string APIPathAirline = "airline/iata/";

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
                        // Getting Flights for a period of 3 months from now

                        DateTime StartDate = DateTime.Now;
                        DateTime EndDate = StartDate.AddDays(90);
                        int DayInterval = 1;
                        while (StartDate.AddDays(DayInterval) <= EndDate)
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
                                    DepartureDateTime = StartDate.ToString("dd/MM/yyyy"),
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

                                        // Parse date flight to day
                                        string TEMP_DateTime = Convert.ToString(flight.Fecha);
                                        DateTime dateValue = DateTime.Parse(TEMP_DateTime);
                                        Boolean TEMP_FlightMonday = false;
                                        Boolean TEMP_FlightTuesday = false;
                                        Boolean TEMP_FlightWednesday = false;
                                        Boolean TEMP_FlightThursday = false;
                                        Boolean TEMP_FlightFriday = false;
                                        Boolean TEMP_FlightSaterday = false;
                                        Boolean TEMP_FlightSunday = false;


                                        int TEMP_Conversie = Convert.ToInt32(dateValue.DayOfWeek);
                                        if (TEMP_Conversie == 1) { TEMP_FlightSunday = true; }
                                        if (TEMP_Conversie == 2) { TEMP_FlightMonday = true; }
                                        if (TEMP_Conversie == 3) { TEMP_FlightTuesday = true; }
                                        if (TEMP_Conversie == 4) { TEMP_FlightWednesday = true; }
                                        if (TEMP_Conversie == 5) { TEMP_FlightThursday = true; }
                                        if (TEMP_Conversie == 6) { TEMP_FlightFriday = true; }
                                        if (TEMP_Conversie == 7) { TEMP_FlightSaterday = true; }



                                        CIFLights.Add(new CIFLight
                                        {
                                            FromIATA = flight.SiglaCiudadOrigen,
                                            FromIATARegion = "",
                                            FromIATACountry = "",
                                            FromIATATerminal = "",
                                            ToIATA = flight.SiglaCiudadDestino,
                                            ToIATACountry = "",
                                            ToIATARegion = "",
                                            ToIATATerminal = "",
                                            FromDate = flight.Fecha,
                                            ToDate = flight.Fecha,
                                            ArrivalTime = flight.Llegada,
                                            DepartTime = flight.Salida,
                                            FlightAircraft = flight.Nombre,
                                            FlightAirline = "1DA",
                                            FlightMonday = TEMP_FlightMonday,
                                            FlightTuesday = TEMP_FlightTuesday,
                                            FlightWednesday = TEMP_FlightWednesday,
                                            FlightThursday = TEMP_FlightThursday,
                                            FlightFriday = TEMP_FlightFriday,
                                            FlightSaterday = TEMP_FlightSaterday,
                                            FlightSunday = TEMP_FlightSunday,
                                            FlightNumber = flight.NumeroVuelo,
                                            FlightOperator = "",
                                            FlightCodeShare = false,
                                            FlightNextDayArrival = false,
                                            FlightNextDays = 0,
                                            FlightNonStop = true,
                                            FlightVia = ""
                                        });

                                        // Prices                                     
                                        // /app/api/TarifaWeb?cupos=1&fechaVuelo=2016-12-28T00:00:00&idVuelo=86056
                                    }
                                }
                            }
                            // End Date loop
                            StartDate = StartDate.AddDays(DayInterval);
                        }

                        
                        //
                    }
                }
            }

            // Output
            // You'll do something else with it, here I write it to a console window
            // Console.WriteLine(text.ToString());

            // Write the list of objects to a file.
            System.Xml.Serialization.XmlSerializer writer =
            new System.Xml.Serialization.XmlSerializer(CIFLights.GetType());
            string myDir = AppDomain.CurrentDomain.BaseDirectory + "\\output";
            Directory.CreateDirectory(myDir);
            StreamWriter file =
               new System.IO.StreamWriter("output\\output.xml");

            writer.Serialize(file, CIFLights);
            file.Close();

            Console.WriteLine("Generate GTFS Files...");

            string gtfsDir = AppDomain.CurrentDomain.BaseDirectory + "\\gtfs";
            System.IO.Directory.CreateDirectory(gtfsDir);
            Console.WriteLine("Creating GTFS File agency.txt...");

            using (var gtfsagency = new StreamWriter(@"gtfs\\agency.txt"))
            {
                var csv = new CsvWriter(gtfsagency);
                csv.Configuration.Delimiter = ",";
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.Configuration.TrimFields = true;
                // header 
                csv.WriteField("agency_id");
                csv.WriteField("agency_name");
                csv.WriteField("agency_url");
                csv.WriteField("agency_timezone");
                csv.WriteField("agency_lang");
                csv.WriteField("agency_phone");
                csv.WriteField("agency_fare_url");
                csv.WriteField("agency_email");
                csv.NextRecord();

                var airlines = CIFLights.Select(m => new { m.FlightAirline }).Distinct().ToList();

                for (int i = 0; i < airlines.Count; i++) // Loop through List with for)
                {
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string url = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirline + airlines[i].FlightAirline;
                        var json = client.DownloadString(url);
                        dynamic AirlineResponseJson = JsonConvert.DeserializeObject(json);
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].code));
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].name));
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].website));
                        csv.WriteField("America/Bogota");
                        csv.WriteField("ES");
                        csv.WriteField(Convert.ToString(AirlineResponseJson[0].phone));
                        csv.WriteField("");
                        csv.WriteField("");
                        csv.NextRecord();

                    }
                }
            }

            Console.WriteLine("Creating GTFS File routes.txt ...");

            using (var gtfsroutes = new StreamWriter(@"gtfs\\routes.txt"))
            {
                // Route record
                var csvroutes = new CsvWriter(gtfsroutes);
                csvroutes.Configuration.Delimiter = ",";
                csvroutes.Configuration.Encoding = Encoding.UTF8;
                csvroutes.Configuration.TrimFields = true;
                // header 
                csvroutes.WriteField("route_id");
                csvroutes.WriteField("agency_id");
                csvroutes.WriteField("route_short_name");
                csvroutes.WriteField("route_long_name");
                csvroutes.WriteField("route_desc");
                csvroutes.WriteField("route_type");
                csvroutes.WriteField("route_url");
                csvroutes.WriteField("route_color");
                csvroutes.WriteField("route_text_color");
                csvroutes.NextRecord();

                var routes = CIFLights.Select(m => new { m.FromIATA, m.ToIATA, m.FlightAirline }).Distinct().ToList();

                for (int i = 0; i < routes.Count; i++) // Loop through List with for)
                {
                    string FromAirportName = null;
                    string ToAirportName = null;
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string url = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + routes[i].FromIATA;
                        var json = client.DownloadString(url);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(json);
                        FromAirportName = Convert.ToString(AirportResponseJson[0].name);
                    }
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string url = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + routes[i].ToIATA;
                        var json = client.DownloadString(url);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(json);
                        ToAirportName = Convert.ToString(AirportResponseJson[0].name);
                    }

                   
                    csvroutes.WriteField(routes[i].FromIATA + routes[i].ToIATA + routes[i].FlightAirline);
                    csvroutes.WriteField(routes[i].FlightAirline);
                    csvroutes.WriteField("");
                    if (FromAirportName != null & ToAirportName != null)
                    {
                        csvroutes.WriteField(FromAirportName + " - " + ToAirportName);
                    }
                    else
                    {
                        csvroutes.WriteField(routes[i].FromIATA + routes[i].ToIATA + routes[i].FlightAirline);
                    }
                    csvroutes.WriteField(""); // routes[i].FlightAircraft + ";" + CIFLights[i].FlightAirline + ";" + CIFLights[i].FlightOperator + ";" + CIFLights[i].FlightCodeShare
                    // Domestic Flight
                    csvroutes.WriteField(1102);                    
                    csvroutes.WriteField("");
                    csvroutes.WriteField("");
                    csvroutes.WriteField("");
                    csvroutes.NextRecord();
                }
            }

            // stops.txt

            List<string> agencyairportsiata =
                CIFLights.SelectMany(m => new string[] { m.FromIATA, m.ToIATA })
                        .Distinct()
                        .ToList();

            using (var gtfsstops = new StreamWriter(@"gtfs\\stops.txt"))
            {
                // Route record
                var csvstops = new CsvWriter(gtfsstops);
                csvstops.Configuration.Delimiter = ",";
                csvstops.Configuration.Encoding = Encoding.UTF8;
                csvstops.Configuration.TrimFields = true;
                // header                                 
                csvstops.WriteField("stop_id");
                csvstops.WriteField("stop_name");
                csvstops.WriteField("stop_desc");
                csvstops.WriteField("stop_lat");
                csvstops.WriteField("stop_lon");
                csvstops.WriteField("zone_id");
                csvstops.WriteField("stop_url");
                csvstops.WriteField("stop_timezone");
                csvstops.NextRecord();

                for (int i = 0; i < agencyairportsiata.Count; i++) // Loop through List with for)
                {
                    // Using API for airport Data.
                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        string url = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + agencyairportsiata[i];
                        var json = client.DownloadString(url);
                        dynamic AirportResponseJson = JsonConvert.DeserializeObject(json);

                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].code));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].name));
                        csvstops.WriteField("");
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].lat));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].lng));
                        csvstops.WriteField("");
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].website));
                        csvstops.WriteField(Convert.ToString(AirportResponseJson[0].timezone));
                        csvstops.NextRecord();
                    }
                }
            }

            Console.WriteLine("Creating GTFS File trips.txt and stop_times.txt...");
            using (var gtfscalendar = new StreamWriter(@"gtfs\\calendar.txt"))
            {
                using (var gtfstrips = new StreamWriter(@"gtfs\\trips.txt"))
                {
                    using (var gtfsstoptimes = new StreamWriter(@"gtfs\\stop_times.txt"))
                    {
                        // Headers 
                        var csvstoptimes = new CsvWriter(gtfsstoptimes);
                        csvstoptimes.Configuration.Delimiter = ",";
                        csvstoptimes.Configuration.Encoding = Encoding.UTF8;
                        csvstoptimes.Configuration.TrimFields = true;
                        // header 
                        csvstoptimes.WriteField("trip_id");
                        csvstoptimes.WriteField("arrival_time");
                        csvstoptimes.WriteField("departure_time");
                        csvstoptimes.WriteField("stop_id");
                        csvstoptimes.WriteField("stop_sequence");
                        csvstoptimes.WriteField("stop_headsign");
                        csvstoptimes.WriteField("pickup_type");
                        csvstoptimes.WriteField("drop_off_type");
                        csvstoptimes.WriteField("shape_dist_traveled");
                        csvstoptimes.WriteField("timepoint");
                        csvstoptimes.NextRecord();

                        var csvtrips = new CsvWriter(gtfstrips);
                        csvtrips.Configuration.Delimiter = ",";
                        csvtrips.Configuration.Encoding = Encoding.UTF8;
                        csvtrips.Configuration.TrimFields = true;
                        // header 
                        csvtrips.WriteField("route_id");
                        csvtrips.WriteField("service_id");
                        csvtrips.WriteField("trip_id");
                        csvtrips.WriteField("trip_headsign");
                        csvtrips.WriteField("trip_short_name");
                        csvtrips.WriteField("direction_id");
                        csvtrips.WriteField("block_id");
                        csvtrips.WriteField("shape_id");
                        csvtrips.WriteField("wheelchair_accessible");
                        csvtrips.WriteField("bikes_allowed ");
                        csvtrips.NextRecord();

                        var csvcalendar = new CsvWriter(gtfscalendar);
                        csvcalendar.Configuration.Delimiter = ",";
                        csvcalendar.Configuration.Encoding = Encoding.UTF8;
                        csvcalendar.Configuration.TrimFields = true;
                        // header 
                        csvcalendar.WriteField("service_id");
                        csvcalendar.WriteField("monday");
                        csvcalendar.WriteField("tuesday");
                        csvcalendar.WriteField("wednesday");
                        csvcalendar.WriteField("thursday");
                        csvcalendar.WriteField("friday");
                        csvcalendar.WriteField("saturday");
                        csvcalendar.WriteField("sunday");
                        csvcalendar.WriteField("start_date");
                        csvcalendar.WriteField("end_date");
                        csvcalendar.NextRecord();

                        //1101 International Air Service
                        //1102 Domestic Air Service
                        //1103 Intercontinental Air Service
                        //1104 Domestic Scheduled Air Service

                        for (int i = 0; i < CIFLights.Count; i++) // Loop through List with for)
                        {

                            // Calender

                            csvcalendar.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightMonday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightTuesday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightWednesday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightThursday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightFriday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightSaterday));
                            csvcalendar.WriteField(Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvcalendar.WriteField(String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate));
                            csvcalendar.WriteField(String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate));
                            csvcalendar.NextRecord();

                            // Trips
                            string FromAirportName = null;
                            string ToAirportName = null;
                            using (var client = new WebClient())
                            {
                                client.Encoding = Encoding.UTF8;
                                string url = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + CIFLights[i].FromIATA;
                                var json = client.DownloadString(url);
                                dynamic AirportResponseJson = JsonConvert.DeserializeObject(json);
                                FromAirportName = Convert.ToString(AirportResponseJson[0].name);
                            }
                            using (var client = new WebClient())
                            {
                                client.Encoding = Encoding.UTF8;
                                string url = ConfigurationManager.AppSettings.Get("APIUrl") + APIPathAirport + CIFLights[i].ToIATA;
                                var json = client.DownloadString(url);
                                dynamic AirportResponseJson = JsonConvert.DeserializeObject(json);
                                ToAirportName = Convert.ToString(AirportResponseJson[0].name);
                            }

                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline);
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvtrips.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvtrips.WriteField(ToAirportName);
                            csvtrips.WriteField(CIFLights[i].FlightNumber);
                            csvtrips.WriteField("");
                            csvtrips.WriteField("");
                            csvtrips.WriteField("");
                            csvtrips.WriteField("1");
                            csvtrips.WriteField("");
                            csvtrips.NextRecord();

                            // Depart Record
                            csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                            csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].DepartTime));
                            csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].DepartTime));
                            csvstoptimes.WriteField(CIFLights[i].FromIATA);
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField(ToAirportName);
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("0");
                            csvstoptimes.WriteField("");
                            csvstoptimes.WriteField("");
                            csvstoptimes.NextRecord();
                            // Arrival Record
                            //if(CIFLights[i].DepartTime.TimeOfDay < System.TimeSpan.Parse("23:59:59") && CIFLights[i].ArrivalTime.TimeOfDay > System.TimeSpan.Parse("00:00:00"))
                            if (!CIFLights[i].FlightNextDayArrival)
                            {
                                csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                                csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].ArrivalTime));
                                csvstoptimes.WriteField(String.Format("{0:HH:mm:ss}", CIFLights[i].ArrivalTime));
                                csvstoptimes.WriteField(CIFLights[i].ToIATA);
                                csvstoptimes.WriteField("2");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("");
                                csvstoptimes.NextRecord();
                            }
                            else
                            {
                                //add 24 hour for the gtfs time

                                int hour = CIFLights[i].ArrivalTime.Hour;
                                hour = hour + 24;
                                int minute = CIFLights[i].ArrivalTime.Minute;
                                string strminute = minute.ToString();
                                if (strminute.Length == 1) { strminute = "0" + strminute; }
                                csvstoptimes.WriteField(CIFLights[i].FromIATA + CIFLights[i].ToIATA + CIFLights[i].FlightAirline + CIFLights[i].FlightNumber.Replace(" ", "") + String.Format("{0:yyyyMMdd}", CIFLights[i].FromDate) + String.Format("{0:yyyyMMdd}", CIFLights[i].ToDate) + Convert.ToInt32(CIFLights[i].FlightMonday) + Convert.ToInt32(CIFLights[i].FlightTuesday) + Convert.ToInt32(CIFLights[i].FlightWednesday) + Convert.ToInt32(CIFLights[i].FlightThursday) + Convert.ToInt32(CIFLights[i].FlightFriday) + Convert.ToInt32(CIFLights[i].FlightSaterday) + Convert.ToInt32(CIFLights[i].FlightSunday));
                                csvstoptimes.WriteField(hour + ":" + strminute + ":00");
                                csvstoptimes.WriteField(hour + ":" + strminute + ":00");
                                csvstoptimes.WriteField(CIFLights[i].ToIATA);
                                csvstoptimes.WriteField("2");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("0");
                                csvstoptimes.WriteField("");
                                csvstoptimes.WriteField("");
                                csvstoptimes.NextRecord();
                            }
                        }
                    }
                }
            }
            // Create Zip File
            string startPath = gtfsDir;
            string DataDir = AppDomain.CurrentDomain.BaseDirectory + "\\data";
            System.IO.Directory.CreateDirectory(DataDir);
            string zipPath = DataDir + "\\ADA.zip";

            if (File.Exists(zipPath)) { File.Delete(zipPath); }
            ZipFile.CreateFromDirectory(startPath, zipPath, CompressionLevel.Fastest, false);




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

        [Serializable]
        public class CIFLight
        {
            // Auto-implemented properties. 

            public string FromIATA;
            public string FromIATACountry;
            public string FromIATARegion;
            public string FromIATATerminal;
            public string ToIATA;
            public string ToIATACountry;
            public string ToIATARegion;
            public string ToIATATerminal;
            public DateTime FromDate;
            public DateTime ToDate;
            public Boolean FlightMonday;
            public Boolean FlightTuesday;
            public Boolean FlightWednesday;
            public Boolean FlightThursday;
            public Boolean FlightFriday;
            public Boolean FlightSaterday;
            public Boolean FlightSunday;
            public DateTime DepartTime;
            public DateTime ArrivalTime;
            public String FlightNumber;
            public String FlightAirline;
            public String FlightOperator;
            public String FlightAircraft;
            public Boolean FlightCodeShare;
            public Boolean FlightNextDayArrival;
            public int FlightNextDays;
            public string FlightDuration;
            public Boolean FlightNonStop;
            public string FlightVia;
        }


    }
}
