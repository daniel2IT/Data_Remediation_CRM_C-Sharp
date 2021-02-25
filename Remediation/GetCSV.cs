using Data;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UpdateRequest = Microsoft.Xrm.Sdk.Messages.UpdateRequest;

namespace Remediation
{
    public class GetCSV  : ITasks
    {
        private static StringBuilder logReader = new StringBuilder();
        public StringBuilder StartUp(IOrganizationService service)
        {
            DataTable csvData = ReadCSV();
            DataTable badData = csvData.Clone();

            CreateAccount(service, csvData, badData);
            CreateEmployee(service, csvData, badData);

            ImportBadData(badData);
            return logReader;
        }
        public static DataTable ReadCSV()
        {
            try
            {
                DataTable csvData = new DataTable("DataTable");
                csvData.Columns.Add(new DataColumn("Account_Name", typeof(string)));
                csvData.Columns.Add(new DataColumn("Employee_Lastname", typeof(string)));
                csvData.Columns.Add(new DataColumn("Salary", typeof(string)));

                var csvParth = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName + $"\\CSV\\EmployeesSalary.csv";
                csvData = TrySetCSV(csvData, csvParth);

                logReader.AppendLine("CSV Data Is Successfully Loaded...");

                return csvData;
            }
            catch (Exception ex)
            {
                logReader.AppendLine(ex.ToString());
                throw;
            }
        }
        public static DataTable TrySetCSV(DataTable tableStructure, string filepath)
        {
            var lines = File.ReadAllLines(filepath, Encoding.UTF8);            
           
            for (int line = 0; line < lines.Length; line++) 
            {
                if (line != 0)
                {
                    var value = lines[line].Split(',');
                      
                    value[2] = Regex.Replace(value[2], @"[^0-9.]+", "");

                    tableStructure.Rows.Add(value);
                }
            }
            return tableStructure;
        }
        public static void CreateAccount(IOrganizationService service, DataTable csvData, DataTable badData)
        {
            ExecuteMultipleRequest executeMultiple = HelperClass.MultipleRequestSetUp();

            List<string> accounts = csvData.AsEnumerable().Select(toList => toList["Account_Name"].ToString()).Distinct().ToList();

            List<string> accNameArray = new List<string>();

            foreach (string account in accounts.Where(x => x != null))
            {
                if (EntityCollection(service, "new_course_account", account).Entities.Count == 0)
                {
                    Entity accountEntity = new Entity("new_course_account");
                    accountEntity["new_name"] = account;
                    accNameArray.Add(account);

                    CreateRequest createRequest = new CreateRequest { Target = accountEntity };
                    executeMultiple.Requests.Add(createRequest);
                }
            }

            ExecuteMultipleResponse executeMultipleResponses = (ExecuteMultipleResponse)service.Execute(executeMultiple);
            ExecuteMultipleChecker(executeMultipleResponses, null, badData, accNameArray);

            logReader.AppendFormat("Accounts -  Is Successfully Loaded To CRM Data...{0}", Environment.NewLine);
        }
        public void DataTable ExecuteMultipleChecker(ExecuteMultipleResponse executeMultipleResponses, DataTable csvData, DataTable badData, List<string> accNameArray)
        {
            var repeat = 0;
            foreach (var checkResponse in executeMultipleResponses.Responses)
            {
                if (checkResponse.Fault != null)
                {
                    DataRow badDataWrite = badData.NewRow();
                    logReader.AppendFormat($"import Error in {repeat} index " + checkResponse.Fault.Message + " {0}", Environment.NewLine);

                    if(accNameArray == null) 
                    { 
                        badDataWrite["Account_Name"] = csvData.Rows[repeat]["Account_Name"];
                        badDataWrite["Employee_Lastname"] = csvData.Rows[repeat]["Employee_Lastname"];
                        badDataWrite["Salary"] = csvData.Rows[repeat]["Salary"];
                    }
                    else
                    {
                      badDataWrite["Account_Name"] = accNameArray[repeat];
                    }
                    badData.Rows.Add(badDataWrite);
                }
                repeat++;
            }
        }
        public static void ImportBadData(DataTable badData)
        {
            StringBuilder dataWriter = new StringBuilder();

            IEnumerable<string> columnNames = badData.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            dataWriter.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in badData.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                dataWriter.AppendLine(string.Join(",", fields));
            }
            File.WriteAllText("WrongData.csv", dataWriter.ToString());
        }
        public static void CreateEmployee(IOrganizationService service, DataTable csvData, DataTable badData)
        {
            var executeMultiple = HelperClass.MultipleRequestSetUp();

            foreach (DataRow data in csvData.Rows)
            {
                Entity accountEmployee = new Entity("new_course_accountemployee");

                Boolean IsCreate;
                if (EntityCollection(service, accountEmployee.LogicalName, data["Employee_Lastname"].ToString().Trim()).Entities.Count >= 1)
                {
                    accountEmployee = new Entity("new_course_accountemployee", (Guid)EntityCollection(service, accountEmployee.LogicalName, data["Employee_Lastname"].ToString().Trim())[0].Attributes["new_course_accountemployeeid"]);
                    IsCreate = false;
                }
                else
                {
                    accountEmployee = new Entity("new_course_accountemployee");
                    IsCreate = true;
                }
                accountEmployee["new_name"] = data.ItemArray[1];
                accountEmployee["new_lastname"] = data.ItemArray[1];

                try
                {
                    if (IsCreate.Equals(true))
                    {
                        CreateRequest reqCreate = new CreateRequest { Target = accountEmployee };
                        executeMultiple.Requests.Add(reqCreate);
                    }
                    else
                    {
                        UpdateRequest reqUpdate = new UpdateRequest { Target = accountEmployee };
                        executeMultiple.Requests.Add(reqUpdate);
                    }

                    SetEmployeeData(accountEmployee, data, service);
                }
                catch
                {
                    continue;
                }
            }
            ExecuteMultipleResponse executeMultipleResponses = (ExecuteMultipleResponse)service.Execute(executeMultiple);

            ExecuteMultipleChecker(executeMultipleResponses, csvData, badData, null);
            logReader.AppendFormat("Employees -  Is Successfully Loaded To CRM Data...{0}", Environment.NewLine);
        }
        public static Entity SetEmployeeData(Entity accountEmployee, DataRow data, IOrganizationService service)
        {
            var LookupId = (Guid)EntityCollection(service, "new_course_account", data["Account_Name"].ToString().Trim())[0].Attributes["new_course_accountid"];
            
            accountEmployee["new_account"] = new EntityReference("new_course_account", LookupId);
            accountEmployee["new_salary"] = new Money(Math.Round(Convert.ToDecimal(data.ItemArray[2].ToString()), 2));

            return accountEmployee;
        }
        public static EntityCollection EntityCollection(IOrganizationService service, string logicalName, string specificName)
        {
            QueryExpression query = new QueryExpression { EntityName = logicalName, ColumnSet = new ColumnSet("new_name") };

            query.Criteria.AddCondition("new_name", ConditionOperator.Equal, specificName);

            return service.RetrieveMultiple(query);
        }
    }
}
