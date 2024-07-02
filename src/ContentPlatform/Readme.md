This repo is used as a playground for .NET Aspire. The app is built up of two services:

- Newsletter.Api: A simple API that allows posting a newsletter.
- Newsletter.Report.Api: Newsletter.Api will write its newsletter to a database and publish a created event to this API. 

The two-service approach is used to check:

- How Aspire can be used to spin up the two APIs, including their dependencies.
- If Aspire is capable of setting up the internal network between the two services.
- If Aspire is able to collect open telemetry data from the two services.
