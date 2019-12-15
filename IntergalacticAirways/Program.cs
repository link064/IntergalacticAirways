using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace IntergalacticAirways
{
    class Program
    {
        static Dictionary<string, int> shipsAndPassengers = new Dictionary<string, int>();
        static readonly HttpClient _httpClient = new HttpClient();

        static void Main(string[] args)
        {
            string input = null;

            // Initialize lists
            try
            {
                InitStarshipsAndPilots();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Welcome to Intergalactic Airways, the premier service for coordinating your transportation needs!");
            Console.WriteLine("Please enter the number of passengers that need transportation below or type \"exit\" to leave.");

            while (input?.ToLower() != "exit")
            {
                Console.Write(": ");
                input = Console.ReadLine();
                // Check that it's a number
                if (int.TryParse(input, out int numPassengers) && numPassengers >= 0)
                {
                    // Get list of starships and pilots for the number of passengers
                    var results = shipsAndPassengers.Where(ship => ship.Value >= numPassengers).Select(ship => ship.Key).ToList();
                    if (results == null || results.Count == 0)
                        Console.WriteLine($"No starships found that can carry {numPassengers} passengers.");
                    else
                    {
                        Console.WriteLine("\r\n--------------------------------------");
                        Console.WriteLine("The following ship and pilot combinations can carry enough passengers:\r\n");
                        results.ForEach(s => Console.WriteLine(s));
                        Console.WriteLine("--------------------------------------\r\n");
                    }
                }
                else if(input?.ToLower() != "exit")
                    Console.WriteLine("That's not how the Force works!\r\n");
            }

            Console.WriteLine("Thank you for using Intergalactic Airways and may the Force be with you!");
            Console.ReadKey();
        }

        /// <summary>
        /// Centralized method for retrieving data from the SW API
        /// </summary>
        /// <param name="nextPage">URL for the next page of data</param>
        /// <param name="action">Action to perform while iterating over results</param>
        private static void GetData(string nextPage, Action<JToken> action)
        {
            while (!string.IsNullOrEmpty(nextPage))
            {
                var response = _httpClient.GetAsync(nextPage).Result;

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Something went wrong with the API: ({response.StatusCode}) {response.ReasonPhrase}");

                var data = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                nextPage = data["next"].ToString(); // Save the next page url
                var results = data["results"];
                if (results != null && results.Count() > 0)
                {
                    foreach (var dataItem in data["results"])
                    {
                        action(dataItem);
                    }
                }
            }
        }

        /// <summary>
        /// Sets up the cached data for starships and pilots
        /// </summary>
        private static void InitStarshipsAndPilots()
        {
            const string apiUrl = "https://swapi.co/api/";
            Dictionary<string, string> pilots = new Dictionary<string, string>();

            // Get the pilots first
            GetData(apiUrl + "people/?page=1", person => pilots.Add(person["url"].ToString(), person["name"].ToString()));

            GetData(apiUrl + "starships/?page=1", starship =>
            {
                if (starship["pilots"] != null && starship["pilots"].Count() > 0 && int.TryParse(starship["passengers"].ToString(), out int numPassengers))
                {
                    foreach (var pilot in starship["pilots"])
                    {
                        if (pilots.ContainsKey(pilot.ToString()))
                        {
                            shipsAndPassengers.Add($"{starship["name"]} - {pilots[pilot.ToString()]}", numPassengers);
                        }
                    }
                }
            });
        }
    }
}
