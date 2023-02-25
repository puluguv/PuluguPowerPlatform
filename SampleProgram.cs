using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PowerApps.Samples
{
    public partial class SampleProgram
    {

        //How many records to create with this sample.
        private static readonly int numberOfRecords = 10000;

        [STAThread] // Added to support UX
        private static void Main()
        {

            #region Optimize Connection settings

            //Change max connections from .NET to a remote service default: 2
            System.Net.ServicePointManager.DefaultConnectionLimit = 65000;
            //Bump up the min threads reserved for this app to ramp connections faster - minWorkerThreads defaults to 4, minIOCP defaults to 4
            System.Threading.ThreadPool.SetMinThreads(500, 500);
            //Turn off the Expect 100 to continue message - 'true' will cause the caller to wait until it round-trip confirms a connection to the server
            System.Net.ServicePointManager.Expect100Continue = false;
            //Can decreas overall transmission overhead but can cause delay in data packet arrival
            System.Net.ServicePointManager.UseNagleAlgorithm = false;

            #endregion Optimize Connection settings

            CrmServiceClient service = null;

            try
            {

                var clientId = "<<CLIENT-ID>>";
                var secret = "<<CLIENT-SECRET>>";
                var dataVerseUrl = "<<ORG-URL>>";

                var connectionString = $"AuthType=ClientSecret;" +
                                       $"url={dataVerseUrl};" +
                                       $"ClientId={clientId};" +
                                       $"ClientSecret={secret};" +
                                       $"RequireNewInstance=false;";

                service = new CrmServiceClient(connectionString);


                if (service.IsReady)
                {
                    #region Code

                    ////////////////////////////////////

                    #region Demonstrate


                    try
                    {

                        var startCreate = DateTime.Now;

                        //Retrieve records to update 
                        var entitiesToUpdate = GetEntitiesToUpdate(service);


                        //Import the list of entities
                        var updatedEntities = UpdateEntities(service, entitiesToUpdate.Entities);

                        //capture time for execution
                        var secondsToCreate = (DateTime.Now - startCreate).TotalSeconds;

                        Console.WriteLine($"updated {updatedEntities.Count} entities in  {Math.Round(secondsToCreate)} seconds.");


                    }
                    catch (AggregateException)
                    {
                        // Handle exceptions
                    }

                    Console.WriteLine("Done.");
                    Console.ReadLine();
                }


                #endregion Demonstrate

                #endregion Code

                else
                {
                    const string UNABLE_TO_LOGIN_ERROR = "Unable to Login to Microsoft Dataverse";
                    if (service.LastCrmError.Equals(UNABLE_TO_LOGIN_ERROR))
                    {
                        Console.WriteLine("Check the connection string values in cds/App.config.");
                        throw new Exception(service.LastCrmError);
                    }
                    else
                    {
                        throw service.LastCrmException;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (service != null)
                    service.Dispose();

                Console.WriteLine("Press <Enter> to exit.");
                Console.ReadLine();
            }
        }



    }
}