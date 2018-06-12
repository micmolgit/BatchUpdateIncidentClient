using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using DCRM_Utils;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;

namespace BatchUpdateIncidentClient
{
    public class BatchUpdateIncidentClient
    {
        public string OutputDir { get; set; }
        public string OutputFile { get; set; }
        public string OutputFilePath { get; set; }
        public bool IsDebugMode { get; set; }

        public BatchUpdateIncidentClient()
        {
            this.LoadConfiguration();
        }

        private IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            var configuration = builder.Build();

            return configuration;
        }
        private void LoadConfiguration()
        {
            var configuration = GetConfiguration();

            OutputDir = configuration["OutputDir"];
            OutputFile = configuration["OutputFile"];
            OutputFilePath = string.Format($@"{OutputDir}\{OutputFile}");
            IsDebugMode = configuration["IsDebugMode"] == "true";
        }
        public async Task<List<Guid>> UpdateRelatedIncidentsAsync(Guid incidentId, Guid newAccountId)
        {
            var ctx = DcrmConnectorFactory.GetContext();

            var iDLookupKey = "accountid";
            var entityLookupKey = "incidentid";

            var entityQuery = from incident in ctx.CreateQuery("incident")
                              join account in ctx.CreateQuery("account")
                                on incident["customerid"] equals account["accountid"]
                              where incident[entityLookupKey].Equals(incidentId)
                              select new
                              {
                                  Guid = (Guid)incident[iDLookupKey]
                              };

            List<Guid> relatedincident = new List<Guid>();
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
                        //relatedincident.Add(guid.Guid);
                        var updateRequest = UpdateIncident(guid.Guid, newAccountId);
                        multipleRequest.Requests.Add(updateRequest);
                    }

                    ExecuteMultipleResponse multipleResponse = (ExecuteMultipleResponse)ctx.Execute(multipleRequest);
                });
            }
            catch (Exception ex)
            {
                MiscHelper.WriteLine($"GetRelatedIncidentsAsync() : {ex.Message}");
            }

            return relatedincident;
        }

        private UpdateRequest UpdateIncident(Guid incidentId, Guid accountID)
        {
            var ctx = DcrmConnectorFactory.GetContext();

            var account = new EntityReference(Account.EntityLogicalName, accountID);

            var incident = new Entity(Incident.EntityLogicalName, incidentId);

            //Populate whatever fields you want (this is just an example)
            incident["customerid"] = accountID;

            UpdateRequest updateRequest = new UpdateRequest { Target = incident };

            return updateRequest;
        }
    }
}