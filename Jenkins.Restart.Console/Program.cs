using System;
using System.Collections.Generic;
using System.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System.Linq;
using System.Net;
using System.ServiceProcess;

namespace Jenkins.Restart.Console
{
    class Program
    {
        private static RestClient _client;
        static void Main(string[] args)
        {
            var url = ConfigurationManager.AppSettings["url"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];

            System.Console.WriteLine($"Restarting {url}");

            _client = new RestClient(url)
            {
                Authenticator = new HttpBasicAuthenticator(username, password)
            };

            if (!JobsAreQueued())
            {
                System.Console.WriteLine($"There are no jobs queued, restarting service");
                Restart();
                return;
            }
            System.Console.WriteLine($"There are jobs queued or could not communicate with Jenkins, exiting program");
        }

        static void Restart()
        {
            var controller = new ServiceController
            {
                MachineName = ".",
                ServiceName = "Jenkins"
            };

            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped);
            System.Console.WriteLine($"stopped service");
            controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running);
            System.Console.WriteLine($"started service, Jenkins is starting now....");
        }

        static bool JobsAreQueued()
        {
            var request = new RestRequest("/queue/api/json", Method.GET);
            var response = _client.Execute<JenkinsQueue>(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                System.Console.WriteLine($"could not communicate with Jenkins");
                return true;
            }
            return response.Data.items.Any();
        }
    }


    public class JenkinsQueue
    {
        public List<Item> items { get; set; }

        public class Item
        {
            public int id { get; set; }
        }
    }
}