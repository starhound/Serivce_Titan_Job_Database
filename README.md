# Serivce_Titan_Job_Database
Console program to grab information about jobs from [Service Titan](https://servicetitan.com) and plug them into a SQL database.

*DEPRCIATED DUE TO SERVICE TITAN REPORTING UPDATES: YOU CAN NOW EXPORT ALL INFORMATION THIS PROGRAM PROVIDES THROUGH THE SERVICE TITAN WEBSITE.*

Provide this program with a text file containing a list of job numbers from Service Titan and it will query their API, grab some information about each job, and plug those values into a database.

You must provide your own SQL Connection String, a Service Titan Account & Password, along with a Service Titan API Key.

With some simple reconfiguration you can alter this to pull additional information regarding each job & update in-house databases with Service Titan job data.
