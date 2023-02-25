using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace PowerApps.Samples
{
    public partial class SampleProgram
    {
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="svc"></param>
        /// <returns></returns>
        private static EntityCollection GetEntitiesToUpdate(CrmServiceClient svc)
        {
            QueryExpression queryExpression = new QueryExpression("contact");
            queryExpression.ColumnSet = new ColumnSet("fullname", "firstname", "lastname");
            return svc.RetrieveMultiple(queryExpression);
        }

        /// <summary>
        /// Creates entities in parallel
        /// </summary>
        /// <param name="svc">The CrmServiceClient instance to use</param>
        /// <param name="entities">A List of entities to create.</param>
        /// <returns></returns>
        private static ConcurrentBag<EntityReference> UpdateEntities(CrmServiceClient svc, DataCollection<Entity> entities)
        {
            var updatedEntityReferences = new ConcurrentBag<EntityReference>();

            Parallel.ForEach(entities, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, 
                () =>  
                {
                //Clone the CrmServiceClient for each thread
                    return svc.Clone();
                },
                (entity, loopState, index, threadLocalSvc) =>
                {
                    //Console.WriteLine(loopState);
                    Console.WriteLine(index);
                    // In each thread, update entities and add them to the ConcurrentBag
                    // as EntityReferences

                    string existingFullName = (string)entity.Attributes["fullname"];
                    string newFullName = null;
                    string[] names = existingFullName.Split(' ');
                    
                    
                    if(names.Length>1)
                    {
                        newFullName = names[1] + " " + names[0];//Build logic to handle  more than 2 values in name split
                        entity.Attributes["fullname"] = newFullName;
                    }
                    threadLocalSvc.Update(entity);
                    updatedEntityReferences.Add(
                        new EntityReference(
                            entity.LogicalName,
                            entity.Id)
                        );

                    return threadLocalSvc;
                },
                (threadLocalSvc) =>
                {
                    //Dispose the cloned CrmServiceClient instance
                    if (threadLocalSvc != null)
                    {
                        threadLocalSvc.Dispose();
                    }
                });

            //Return the ConcurrentBag of EntityReferences
            return updatedEntityReferences;
        }

        /// <summary>
        /// Gets web service connection information from the app.config file.
        /// If there is more than one available, the user is prompted to select
        /// the desired connection configuration by name.
        /// </summary>
        /// <returns>A string containing web service connection configuration information.</returns>
        private static string GetServiceConfiguration()
        {
            // Get available connection strings from app.config.
            int count = ConfigurationManager.ConnectionStrings.Count;

            // Create a filter list of connection strings so that we have a list of valid
            // connection strings for Common Data Service only.
            List<KeyValuePair<String, String>> filteredConnectionStrings =
                new List<KeyValuePair<String, String>>();

            for (int a = 0; a < count; a++)
            {
                if (isValidConnectionString(ConfigurationManager.ConnectionStrings[a].ConnectionString))
                    filteredConnectionStrings.Add
                        (new KeyValuePair<string, string>
                            (ConfigurationManager.ConnectionStrings[a].Name,
                            ConfigurationManager.ConnectionStrings[a].ConnectionString));
            }

            // No valid connections strings found. Write out and error message.
            if (filteredConnectionStrings.Count == 0)
            {
                Console.WriteLine("An app.config file containing at least one valid Common Data Service " +
                    "connection string configuration must exist in the run-time folder.");
                Console.WriteLine("\nThere are several commented out example connection strings in " +
                    "the provided app.config file. Uncomment one of them and modify the string according " +
                    "to your Common Data Service installation. Then re-run the sample.");
                return null;
            }

            // If one valid connection string is found, use that.
            if (filteredConnectionStrings.Count == 1)
            {
                return filteredConnectionStrings[0].Value;
            }

            // If more than one valid connection string is found, let the user decide which to use.
            if (filteredConnectionStrings.Count > 1)
            {
                Console.WriteLine("The following connections are available:");
                Console.WriteLine("------------------------------------------------");

                for (int i = 0; i < filteredConnectionStrings.Count; i++)
                {
                    Console.Write("\n({0}) {1}\t",
                    i + 1, filteredConnectionStrings[i].Key);
                }

                Console.WriteLine();

                Console.Write("\nType the number of the connection to use (1-{0}) [{0}] : ",
                    filteredConnectionStrings.Count);
                String input = Console.ReadLine();
                int configNumber;
                if (input == String.Empty) input = filteredConnectionStrings.Count.ToString();
                if (!Int32.TryParse(input, out configNumber) || configNumber > count ||
                    configNumber == 0)
                {
                    Console.WriteLine("Option not valid.");
                    return null;
                }

                return filteredConnectionStrings[configNumber - 1].Value;
            }
            return null;
        }

        /// <summary>
        /// Verifies if a connection string is valid for Common Data Service.
        /// </summary>
        /// <returns>True for a valid string, otherwise False.</returns>
        private static bool isValidConnectionString(String connectionString)
        {
            // At a minimum, a connection string must contain one of these arguments.
            if (connectionString.Contains("Url=") ||
                connectionString.Contains("Server=") ||
                connectionString.Contains("ServiceUri="))
                return true;

            return false;
        }
    }
}