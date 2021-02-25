using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Configuration;
using System.Net;

namespace Data
{
    public class HelperClass
    {
        public static CrmServiceClient getCRMServie()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string connectionString = ConfigurationManager.ConnectionStrings["CRMConnectionString"].ConnectionString;

            CrmServiceClient serviceClient = new CrmServiceClient(connectionString);

            if (serviceClient == null)
            {
                throw new InvalidOperationException(serviceClient.LastCrmError);
            }

            return serviceClient;
        }

        public static ExecuteMultipleRequest MultipleRequestSetUp()
        {
            return new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                  Requests = new OrganizationRequestCollection()
            };
        }

        public static ITasks MagicallyCreateInstance(string className)
        {
            Type type = Type.GetType(className);
            return (ITasks)Activator.CreateInstance(type);
        }
    }
}
