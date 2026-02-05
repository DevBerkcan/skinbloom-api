#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Data.SqlClient, 5.1.5"

using Microsoft.Data.SqlClient;
using System;

var connectionString = "Server=db40004.databaseasp.net,1433; Database=db40004; User Id=db40004; Password=Ht3!%2MiFs9_; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True; Connection Timeout=30;";

Console.WriteLine("Testing SQL Server connection...");
Console.WriteLine($"Server: db40004.databaseasp.net");
Console.WriteLine($"Database: db40004");
Console.WriteLine();

try
{
    using (var connection = new SqlConnection(connectionString))
    {
        Console.WriteLine("Opening connection...");
        connection.Open();
        Console.WriteLine("✅ Connection successful!");
        Console.WriteLine($"Server Version: {connection.ServerVersion}");
        Console.WriteLine($"Database: {connection.Database}");

        // Test query
        using (var command = new SqlCommand("SELECT @@VERSION", connection))
        {
            var result = command.ExecuteScalar();
            Console.WriteLine();
            Console.WriteLine("SQL Server Version:");
            Console.WriteLine(result);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Connection failed!");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Troubleshooting:");
    Console.WriteLine("1. Check if firewall allows outbound connections on port 1433");
    Console.WriteLine("2. Verify server address: db40004.databaseasp.net");
    Console.WriteLine("3. Check MonsterASP control panel if database is active");
    Console.WriteLine("4. Try connecting with SQL Server Management Studio first");
}
