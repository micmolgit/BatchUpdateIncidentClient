using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using DCRM_Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace BatchUpdateIncidentClient
{
    public class BatchUpdateIncidentClient
    {
        #region Properties
        public string OutputDir { get; set; }
        public string OutputFile { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsDebugMode { get; set; }
        #endregion // Properties

        #region BatchUpdateIncidentClient
        public BatchUpdateIncidentClient()
        {
            this.LoadConfiguration();
        }
        #endregion // BatchUpdateIncidentClient

        #region GetConfiguration
        private IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            return configuration;
        }
        #endregion // GetConfiguration

        #region LoadConfiguration
        private void LoadConfiguration()
        {
            var configuration = GetConfiguration();

            OutputDir = configuration["OutputDir"];
            OutputFile = configuration["OutputFile"];
            OutputFilePath = string.Format($@"{OutputDir}\{OutputFile}");
            IsDebugMode = configuration["IsDebugMode"] == "true";
        }
        #endregion // LoadConfiguration

        #region GetIncidentId
        Guid GetIncidentId(Entity entity)
        {
            var Guid = (Guid) entity["incidentid"];
            return Guid;
        }
        #endregion // GetIncidentId

        #region GetCustomerIdFromIncident
        private string GetCustomerIdFromIncident(string incidentId)
        {
            var ctx = DcrmConnectorFactory.GetContext();

            var entityQuery = from entity in ctx.CreateQuery("incident")
                              where entity["incidentid"].Equals(incidentId)
                              select new
                              {
                                  Guid = (EntityReference) entity["customerid"]
                              };

            var guid = "";
            try
            {
                var entity = entityQuery.First();
                if (entity != null)
                {
                    EntityReference reference = (EntityReference) entity.Guid;
                    guid = reference.Id.ToString();
                }
            }
            catch (Exception Ex)
            {
                MiscHelper.WriteLine($"GetGuidFromEntity : {Ex.Message}");
            }

            return guid;
        }
        #endregion // GetCustomerIdFromIncident

        #region UpdateRelatedIncidentsAsync
        public async Task<int> UpdateRelatedIncidentsAsync(Guid incidentId, Guid newAccountId)
        {
            var ctx = DcrmConnectorFactory.GetContext();
            var oldAccountId = GetCustomerIdFromIncident(incidentId.ToString());

            var entityQuery = from incident in ctx.CreateQuery("incident")
                              where incident["customerid"].Equals(oldAccountId)
                              where incident["statecode"].Equals(0)
                              select new
                              {
                                  Guid = GetIncidentId(incident)
                              };
            var relatedIncidentsCount = 0;

            MiscHelper.WriteLine("Querying DCRM Please Wait...");

            try
            {
                await Task<uint>.Run(() =>
                {
                    var multipleRequest = new ExecuteMultipleRequest()
                    {
                        Settings = new ExecuteMultipleSettings()
                        {
                            ContinueOnError = false,
                            ReturnResponses = true
                        },
                        Requests = new OrganizationRequestCollection()
                    };

                    foreach (var guid in entityQuery)
                    {
                        var updateRequest = UpdateIncident(guid.Guid, newAccountId);
                        multipleRequest.Requests.Add(updateRequest);
                        relatedIncidentsCount++;
                    }

                    ExecuteMultipleResponse multipleResponse = (ExecuteMultipleResponse) ctx.Execute(multipleRequest);

                    foreach (var responseItem in multipleResponse.Responses)
                    {
                        // An error has occurred.
                         if (responseItem.Fault != null)
                            MiscHelper.WriteLine($"{multipleResponse.Responses[responseItem.RequestIndex]} : {responseItem.Fault}");
                    }
                });
            }
            catch (Exception ex)
            {
                MiscHelper.WriteLine($"GetRelatedIncidentsAsync() : {ex.Message}");
            }

            return relatedIncidentsCount;
        }
        #endregion // UpdateRelatedIncidentsAsync

        #region UpdateIncident
        private UpdateRequest UpdateIncident(Guid incidentId, Guid accountID)
        {
            var ctx = DcrmConnectorFactory.GetContext();

            var account = new EntityReference(Account.EntityLogicalName, accountID);

            var incident = new Entity(Incident.EntityLogicalName, incidentId);

            incident["customerid"] = account;

            UpdateRequest updateRequest = new UpdateRequest { Target = incident };

            return updateRequest;
        }
        #endregion // UpdateIncident

        #region Terminate
        public void Terminate()
        {
            MiscHelper.PauseExecution();
        }
        #endregion // Terminate
    }
}