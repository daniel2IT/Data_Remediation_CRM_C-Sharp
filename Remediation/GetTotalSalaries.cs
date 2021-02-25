using Data;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Remediation
{
    public class GetTotalSalaries : ITasks
    {
        public StringBuilder StartUp(IOrganizationService conString)
        {
            return GetTotalSalary(conString);
        }
        public static IEnumerable<Entity>  GetEmployees(IOrganizationService service)
        {
            QueryExpression queryEmployee = new QueryExpression("new_course_accountemployee");
            queryEmployee.ColumnSet.AddColumns("new_salary", "new_account");

            queryEmployee.Criteria.AddCondition("statuscode", ConditionOperator.Equal, (1));
            queryEmployee.Criteria.AddCondition("new_salary", ConditionOperator.NotNull);
            queryEmployee.Criteria.AddCondition("new_account", ConditionOperator.NotNull);

            return service.RetrieveMultiple(queryEmployee).Entities;
        }
        private static StringBuilder GetTotalSalary(IOrganizationService service)
        {
            StringBuilder logReader = new StringBuilder();
            var entities = GetEmployees(service);
            var accountRecords = from employee in entities.AsEnumerable()
                        group employee by new
                        {
                                new_account = ((EntityReference)employee.Attributes["new_account"]).Id.ToString()
                        }
                        into account
                        select new
                        {
                            totalSum = account.Sum(x => Convert.ToDecimal(x.GetAttributeValue<Money>("new_salary").Value)),
                            accountId = account.Key.new_account,
                        };
            foreach (var record in accountRecords)
            {
                Entity entityUpdate = new Entity("new_course_account");
                entityUpdate.Id = new Guid(record.accountId);
                entityUpdate.Attributes["new_totalsalaries"] = record.totalSum;

                service.Update(entityUpdate);
                logReader.AppendFormat("Calculated Salary for AccId " + record.accountId + " Finally Got " + record.totalSum + "Eur{0}", Environment.NewLine);
            }
            return logReader;
        }
    }
}