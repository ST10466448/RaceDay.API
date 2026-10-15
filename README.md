Project Overview

RaceDay API is a RESTful web API developed using C# and ASP.NET Core Web API. It provides functionality for managing running, walking and cycling events, user profiles, event categories, participant enrolments and race results.
The API uses SQL Server for data storage, Entity Framework Core for database operations, JWT authentication for securing endpoints, and Swagger UI for documenting and testing the API.

Technologies Used
C#
ASP.NET Core Web API (.NET 8)
SQL Server
Entity Framework Core
JWT Authentication
Swagger / OpenAPI
xUnit
GitHub Actions

User Roles
Participant
Participants can:
Register and log in.
View and update their own profile.
View available events and categories.
Enrol in an event by selecting a category.
View their own race results.

Organiser
Organisers can:
Log in and manage their own profile.
View available events.
Create, update and delete their own events.
Create, update and delete event categories for their events.
View enrolments for their own events.
Capture and manage participant results, including finish times and finishing positions.
Organiser access is controlled through role-based authorisation.

CI Screeenshot
The GitHub Actions workflow automatically builds the RaceDay API
and runs the unit tests whenever changes are pushed.


<img width="773" height="599" alt="image" src="https://github.com/user-attachments/assets/d2000625-9d5d-4f54-a4f9-9b9be03b3906" />
