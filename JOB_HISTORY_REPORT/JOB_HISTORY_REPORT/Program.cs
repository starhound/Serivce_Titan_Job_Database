using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

//TODO: Update app to plug in values for each Bayonet DB table. 

namespace JOB_HISTORY_REPORT
{
    class Program
    {
        //to contain a list of invoice numbers
        static List<int> jobsList = new List<int>();

        //hold responses from ST-API about invoice number get requests
        static List<IRestResponse> jobResponses = new List<IRestResponse>();

        //to contain extracted data from the api return value
        static List<string> returnedData = new List<string>();

        //count of total ivnoices
        static int jobCount = 0;

        //no response from api
        static int skipped = 0;

        //json parse error count
        static int errored = 0;

        static string connectionString = "YOUR_CONNECTION_STRING";
        static string ST_USER = "YOUR USER NAME";
        static string ST_PWD = "YOUR SERVICE TITAN PWD";
        static string API = "YOUR API KEY";
        static string FILE_PATH = "PATH TO INVOICES LIST FILE";

        static string JobGetClientAddress(dynamic jData)
        {
            string addr = jData["customer"]["address"]["street"];
            return addr;
        }

        static IRestResponse GetJobData(int jobNum)
        {
            string requestUrlStart = "jobs/";
            string requestUrlEnd = "?" + API;
            string requestUrlFull = requestUrlStart + jobNum + requestUrlEnd;
            //new rest client for the ST API
            var client = new RestClient("https://api.servicetitan.com/v1");

            //auth factor
            client.Authenticator = new HttpBasicAuthenticator(ST_USER, ST_PWD);

            //initial request
            var request = new RestRequest(requestUrlFull, DataFormat.Json);

            //server response
            IRestResponse response = client.Get(request);

            //grab content of response
            string content = response.Content;

            if (content.Contains("does not exist"))
            {
                Console.WriteLine("JOB #" + jobNum + " DOES NOT EXIST - SKIPPING");
            }
            return response;
        }
        static void InspectJobData(IRestResponse response)
        {
            try
            {
                string content = response.Content;
                if (content.Contains("does not exist"))
                {
                    skipped++;
                    return;
                }
                dynamic Data = JObject.Parse(content);
                JObject jData = Data["data"];
                int jobID = (int)jData["id"];
                int locationID = (int)jData["location"]["id"];
                string jobAddr = (string)jData["customer"]["address"]["street"];
                string name = (string)jData["customer"]["name"];
                int id = (int)jData["customer"]["id"];
                Console.WriteLine("Inserting JobID #" + jobID + " (location: " + locationID + ")");
                SqlConnection sqlConnection1 = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.CommandText = "INSERT Locations (LocationID, JobID, StreetAddr, CustomerName, CustomerID) VALUES (@locationID, @jobID, @jobAddr, @name, @id)";
                cmd.Parameters.AddWithValue("@locationID", locationID);
                cmd.Parameters.AddWithValue("@jobID", jobID);
                cmd.Parameters.AddWithValue("@jobAddr", jobAddr);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Connection = sqlConnection1;
                sqlConnection1.Open();
                cmd.ExecuteNonQuery();
                sqlConnection1.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Main(string[] args)
        {
            String file = FILE_PATH;
            StreamReader dataStream = new StreamReader(file);
            string dataSample;

            //add invoice numbers into list for future iteration
            while ((dataSample = dataStream.ReadLine()) != null)
            {
                if (dataSample.Length == 0)
                    continue;
                int invoice = Int32.Parse(dataSample);
                jobCount++;
                jobsList.Add(invoice);
            }

            int pulledJobs = 1;

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 30 };
            Parallel.ForEach(jobsList, parallelOptions, jobNum =>
            {
                Console.WriteLine("Pulling Job (" + pulledJobs + "/" + jobCount + "): #" + jobNum);
                pulledJobs++;
                IRestResponse content = GetJobData(jobNum);
                InspectJobData(content);
            });

            Console.WriteLine("Skipped Jobs: " + skipped);
            Console.WriteLine("Errored Jobs: " + errored);
        }
    }
}
