<h1><img height="30" src="./Assets/MarsSunRiseJPLNavLogo.webp"/>  MARS</h1>

Managed Account Resources System (MARS) by Moonrise Software LLC

[![Docker Publish](https://github.com/CosmosSoftware/Mars/actions/workflows/docker.yml/badge.svg)](https://github.com/CosmosSoftware/Mars/actions/workflows/docker.yml)

## What is Mars?

Mars is a user account and role resource management website built on the
[ASP.NET Core Identity Framework](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-6.0&tabs=visual-studio). Mars is open source and is free to install and use.  It works with either
Azure Cosmos DB or Microsoft SQL Server.  [Docker container](https://hub.docker.com/repository/docker/toiyabe/mars) installation instructions are below.

Functionality includes:

* User Account Management
  * [Create accounts one at a time or in batches](CreateAccounts.md)
  * Rest passwords
  * Resend confirmation emails
  * Add/Remove multiple users to and from multiple roles
  * Update basic account information
  * Delete accounts
* Role management
  * Create roles
  * Update role names
  * Delete roles
  * Add/Remove multiple users to and from multiple roles

## Capatibility

This product has been tested with the following databases:

* Azure Cosmos Database
* Microsoft SQL Server

## Installation

Follow these steps to install this application:

1. Deploy the docker container to your prefered Docker host.
2. Create a SendGrid account and obtain a SendGrid API Key
3. Set the following configuration variables:

```json
  // Example configuration file
  {
    "ConnectionStrings:ApplicationDbContextConnection": "YOUR ACCOUNT CONNECTION STRING",
    "DatabaseName": "YOUR DATABASE NAME",
    "SendGridApiKey": "YOUR SENDGRID API KEY",
    "SetupDb": "true", // Set to true if you want the database to be setup
    "DbProvider" : "cosmos", // cosmos or mssql
  }
```

```json
  // For MS SQL Server use the following:
  {
    "ConnectionStrings:ApplicationDbContextConnection": "YOUR DB CONNECTION STRING",
    "SendGridApiKey": "YOUR SENDGRID API KEY",
    "SetupDb": "true", // Set to true if you want the database to be setup
    "DbProvider" : "Cosmos", // Cosmos or MSSQL
  }
```

4. Open Mars and create a user account.  The first account is automatically given the "User Administrators" role.

Now log in and you should see one user (yourself), and one role named "User Administrators."
