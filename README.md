Youtube Link: https://www.youtube.com/watch?v=MXQM6khNZK4

1. Project Overview

RaceDay is an event management system developed using C# and ASP.NET Core. It provides a platform for managing running, walking and cycling events.

The solution consists of two applications:

RaceDay API: A RESTful Web API responsible for business logic, data management, authentication and communication with external services.

RaceDay MVC: An ASP.NET Core MVC web application that consumes the API and provides user-friendly interfaces for participants and organisers.

The system uses SQL Server for data storage, Entity Framework Core for database operations, JWT authentication for API security, and Azure Blob Storage for event image uploads.

The MVC application uses session-based authentication and communicates with the API through HTTP requests. It does not directly access the database.

2. Technologies Used

C#

ASP.NET Core Web API (.NET 8)

ASP.NET Core MVC (.NET 8)

SQL Server

Entity Framework Core

JWT Authentication

Session-based authentication

Azure Blob Storage

Swagger / OpenAPI

Docker and Docker Compose

xUnit

Git and GitHub

GitHub Actions

3. User Roles

Participant

Participants can:

Register and log in.

View and update their profiles.

Browse available running, walking and cycling events.

View event details and available categories.

Enrol in events by selecting a category.

View their enrolments.

View their race results.

Organiser

Organisers can:

Register and log in.

View and update their profiles.

Create, update and delete their own events.

Manage event categories.

Upload and manage event images.

View participant enrolments for their events.

Capture and manage participant results, including finishing times and positions.

Role-based authorisation restricts access to the appropriate functionality.

4. Main System Features

Authentication and User Profiles

User registration and login.

Password hashing.

JWT authentication for API requests.

Session-based authentication in the MVC application.

Profile viewing and updating.

Event Management

Create, view, update and delete events.

Support for running, walking and cycling events.

Event descriptions, dates, venues and routes.

Event Categories and Enrolments

Create and manage event categories.

Allow participants to enrol in available categories.

View and manage participant enrolments.

Race Results

Capture participant finishing times and positions.

Manage race results.

Allow participants to view their own results.

Image Management

Upload event images using Azure Blob Storage.

Retrieve and display stored images through the API and MVC application.

API Documentation

Swagger UI provides API endpoint documentation and allows developers to test API requests.

5. Project Structure

The solution contains the following main projects:

RaceDay.API – RESTful API, controllers, models, services and database functionality.

RaceDay.MVC – MVC controllers, views, services and view models.

RaceDay.API.Test – Automated unit tests.

docker-compose.yml – Configuration for running the API and MVC applications together using Docker.

6. Database

RaceDay uses Microsoft SQL Server as its database management system.

Entity Framework Core is used for database operations and communication between the API and SQL Server.

The database stores information relating to:

Users

Routes

Events

Event categories

Participant entries

Race results

The MVC application accesses database information through the RaceDay API rather than connecting directly to SQL Server.

7. Azure Blob Storage

Azure Blob Storage is used to store event images.

The API handles image uploads and retrieval, while the MVC application communicates with the API to upload and display images.

Azure connection details are configured using environment variables or .NET User Secrets. Sensitive connection details are not included in the source code repository.

8. Docker Deployment

Docker and Docker Compose are used to containerise and run both applications.

The root docker-compose.yml file defines two services:

raceday-api

raceday-mvc

Prerequisites

Docker Desktop

Microsoft SQL Server

A restored RaceDay database named RaceDayAPI_DB

An Azure Storage account with Blob Storage access

Environment Configuration

Create a .env file in the repository root, alongside docker-compose.yml.

Add the following variables:

RACEDAY_DB_PASSWORD=YOUR_DATABASE_PASSWORD
AZURE_BLOB_CONNECTION_STRING=YOUR_AZURE_CONNECTION_STRING

Replace the placeholder values with your own credentials.

Important: Never commit or upload the .env file to GitHub.

Running the Application

Open PowerShell in the repository root and run:

docker compose up --build

This builds and starts both the API and MVC containers.

Accessing the Applications

After the containers have started:

RaceDay MVC: http://localhost:5187

RaceDay API Swagger: http://localhost:5289/swagger

To stop the containers, press Ctrl + C in the PowerShell window running Docker Compose.

Alternatively, run:

docker compose down

9. Automated Testing

The project includes an xUnit test project for automated testing.

To execute the tests, open PowerShell in the repository root and run:

dotnet test

The tests cover application functionality and relevant API behaviour.

10. Continuous Integration – GitHub Actions

GitHub Actions is used for continuous integration.

The workflow automatically builds the RaceDay API and executes the automated unit tests when changes are pushed to the GitHub repository.

The workflow results can be viewed under the Actions tab in GitHub.


[Insert YouTube demonstration link]

The demonstration should cover the participant and organiser functionality, API integration, Azure Blob Storage image uploads and Docker deployment.
