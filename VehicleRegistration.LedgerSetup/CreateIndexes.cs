/*
 * Copyright 2021 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify,
 * merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.QLDB.Driver;

namespace VehicleRegistration.LedgerSetup
{
    /// <summary>
    /// <para>Create indexes on tables in a particular ledger.</para>
    ///
    /// <para>
    /// This code expects that you have AWS credentials setup per:
    /// https://docs.aws.amazon.com/sdk-for-net/latest/developer-guide/net-dg-config-creds.html
    /// </para>
    /// </summary>
    public class CreateIndexes
    {
        private IQldbDriver qldbDriver;
        private readonly IValueFactory valueFactory;

        public CreateIndexes(IQldbDriver qldbDriver)
        {
            this.qldbDriver = qldbDriver;
            this.valueFactory = new ValueFactory();
        }

        public async Task Run()
        {
            await Task.Run(async () =>
             {
                 await CreateIndexAsync("VehicleRegistration", "VIN");
                 await CreateIndexAsync("VehicleRegistration", "LicensePlateNumber");
                 await CreateIndexAsync("Vehicle", "VIN");
                 await CreateIndexAsync("Person", "GovId");
                 await CreateIndexAsync("DriversLicense", "LicenseNumber");
                 await CreateIndexAsync("DriversLicense", "PersonId");
             });
        }

        private async Task CreateIndexAsync(string tableName, string field) 
        {
            if (!await CheckIndexExistsAsync(tableName, field))
            {
                Console.WriteLine($"Index does not exists, creating index on {tableName} for {field}.");
                qldbDriver.Execute(transactionExecutor =>
                {
                    transactionExecutor.Execute($"CREATE INDEX ON {tableName}({field})");
                });
            }
            else
            {
                Console.WriteLine($"Index already exists for {tableName} and {field}.");
            }   
        }

        private async Task<bool> CheckIndexExistsAsync(string tableName, string field)
        {
             return await Task.Run(() =>
             {
                 IResult result = qldbDriver.Execute(transactionExecutor =>
                 {
                     IIonValue ionTableName = this.valueFactory.NewString(tableName);
                     IResult result = transactionExecutor.Execute($"SELECT * FROM information_schema.user_tables WHERE name = ?", ionTableName);
                     
                     return result;
                 });

                if (result.Any())
                {
                    IIonList indexes = result.First().GetField("indexes") as IIonList;
                    foreach (IIonValue index in indexes)
                    {
                        string expr = index.GetField("expr").StringValue;
                        if (expr.Contains(field))
                        {
                            return true;
                        }
                    }    
                }

                 return false;
             });
        }
    }
}
