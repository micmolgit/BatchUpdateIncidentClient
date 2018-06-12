using System;
using System.ServiceModel;
using Nito.AsyncEx;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using DCRM_Utils;
using System.Threading;

namespace BatchUpdateIncidentClient
{
    class Program
    {

        #region PrintUsage
        public static void PrintUsage()
        {
            var exeName = Assembly.GetExecutingAssembly().GetName().Name;
            MiscHelper.WriteLine($"Usage :\n" +
                $"- {exeName}.exe <Incident GUID> <Client GUID>");
            MiscHelper.PauseExecution();
        }
        #endregion // PrintUsage

        #region MainAsync
        // We are creating an async main method based on Nito.AsyncEx
        static async Task<bool> MainAsync(string[] args)
        {
            bool isOperationSuccessfull = false;
            Guid incidentId = Guid.Empty;
            Guid newAccountId = Guid.Empty;

            if (args.Length < 2)
            {
                PrintUsage();
                return isOperationSuccessfull;
            }

            BatchUpdateIncidentClient batch = null;

            try
            {
                var exeName = Assembly.GetExecutingAssembly().GetName().Name;
                incidentId = new Guid(args[0]);
                newAccountId = new Guid(args[1]);

                 MiscHelper.WriteLine($"Updating similar incidents as ({incidentId}) with new client ({newAccountId}) )");
            }
            catch (Exception ex)
            {
                MiscHelper.WriteLine($"One of the provided GUID is not valid : {ex.Message}");
                PrintUsage();
            }

            try
            {
                var updatedIncidentsCount = await batch.UpdateRelatedIncidentsAsync(incidentId, newAccountId);
            }
            catch (FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault> ex)
            {
                MiscHelper.WriteLine("The application terminated with an error.");
                MiscHelper.WriteLine($"Timestamp: { ex.Detail.Timestamp}");
                MiscHelper.WriteLine($"Code: {ex.Detail.ErrorCode}");
                MiscHelper.WriteLine($"Message: {ex.Detail.Message}");
                MiscHelper.WriteLine($"Plugin Trace: {ex.Detail.TraceText}");
                if (ex.InnerException != null)
                    MiscHelper.WriteLine($"Inner Fault: { ex.InnerException.Message ?? "No Inner Fault"}");
            }
            catch (System.TimeoutException ex)
            {
                MiscHelper.WriteLine("The application terminated with an error.");
                MiscHelper.WriteLine($"Message: {ex.Message}");
                MiscHelper.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                    MiscHelper.WriteLine($"Inner Fault: { ex.InnerException.Message ?? "No Inner Fault"}");
            }
            catch (System.Exception ex)
            {
                MiscHelper.WriteLine($"The application terminated with an error : {ex.Message}");

                // Display the details of the inner exception.
                if (ex.InnerException != null)
                {
                    MiscHelper.WriteLine(ex.InnerException.Message);

                    if (ex.InnerException is FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault> fe)
                    {
                        MiscHelper.WriteLine($"Timestamp: {fe.Detail.Timestamp}");
                        MiscHelper.WriteLine($"Code: {fe.Detail.ErrorCode}");
                        MiscHelper.WriteLine($"Message: {fe.Detail.Message}");
                        MiscHelper.WriteLine($"Plugin Trace: {fe.Detail.TraceText}");
                        var message = string.Format("Inner Fault: {0}",
                            null == fe.Detail.InnerFault ? "No Inner Fault" : "Has Inner Fault");
                        MiscHelper.WriteLine(message);
                    }
                }
            }
            finally
            {

            }

            return isOperationSuccessfull;
        }
        #endregion //MainAsync

        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }
    }
}
