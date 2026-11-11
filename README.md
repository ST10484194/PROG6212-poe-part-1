# RaceDay - Event Management System

**RaceDay** is a full-stack web-based event management system built for the South African road running, walking, and cycling community. It allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse events, enter them, and track their personal race history.

This is an individual Portfolio of Evidence built in three parts:

| Part | Description | Location |
|---|---|---|
| Part 1 | Planning and database: ERD, endpoint plan, SQL script | `docs/` |
| Part 2 | RESTful ASP.NET Core Web API with EF Core, session authentication, Swagger and unit tests | `RaceDay.Api/` |
| Part 3 | ASP.NET Core MVC web application, Azure Blob Storage and Docker | `RaceDay.Web/` |

## Roles

The system supports two roles. Role-based access is enforced in the API (Part 2) and reflected in the MVC interface (Part 3).

- **Organiser** - can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
- **Participant** - can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

## Repository Structure

```
docs/
├── RaceDay_ERD.png                 Entity Relationship Diagram (Part 1)
├── RaceDay_Endpoint_Plan.md        Full API endpoint plan (Part 1)
├── RaceDay_Database.sql            Database creation and seed script (Part 1)
├── Part2_CI_GreenBuild.png         Part 2 green build screenshot
└── Part3_CI_GreenBuild.png         Part 3 green build screenshot
RaceDay.Api/
├── RaceDay/                        ASP.NET Core Web API (Part 2)
│   ├── Auth/                       RequireLogin attribute (session and role check)
│   ├── Controllers/                Auth, Users, Events, Categories, Enrolments, Results, Uploads
│   ├── Data/                       AppDbContext (EF Core, matches the Part 1 schema)
│   ├── Dtos/                       Request and response classes
│   ├── Migrations/                 EF Core code-first migrations
│   ├── Models/                     Entity classes
│   ├── Services/                   Azure Blob Storage image service (Part 3)
│   └── Program.cs
├── RaceDay.Tests/                  xUnit unit tests
└── RaceDay.slnx                    Solution containing the API, tests and MVC projects
RaceDay.Web/                        ASP.NET Core MVC application (Part 3)
├── Controllers/                    Account, Dashboard, ManageEvents, ManageCategories, ManageEnrolments,
│                                   ManageResults, Events, MyEnrolments, MyResults, ProfilePicture
├── Filters/                        RequireRole attribute (login and role check)
├── Models/                         View models and API data classes
├── Services/                       ApiClient (the only way the MVC app reaches data)
├── Views/                          Razor views and shared layout
├── wwwroot/                        Bootstrap and the RaceDay theme (site.css)
├── Dockerfile
└── .dockerignore
docker-compose.yml                  Builds and starts the MVC container with one command
.github/workflows/
├── part1-validate.yml              Validates the Part 1 repository structure
├── part2-build-test.yml            Builds the API and runs all unit tests
└── part3-ci.yml                    Builds the MVC project, runs it in Docker, and runs the API tests
```

---

# Part 1 - Planning and Database

Part 1 contains the system planning: an Entity Relationship Diagram (ERD), a full API endpoint plan, and a SQL database script.

**Part 1 build status:**

<img width="1431" height="450" alt="Screenshot 2026-09-04 134646" src="https://github.com/user-attachments/assets/afc33081-7a55-49b4-9430-7f6d7f7d93bc" />

### Part 1 video walkthrough

[Watch the Part 1 walkthrough on YouTube](https://youtu.be/PVn1d24QPz0)

---

# Part 2 - RESTful API

The API is built with ASP.NET Core Web API (C#), Entity Framework Core (code-first) and SQL Server. It follows the endpoint plan from Part 1, uses session-based authentication with two roles, is documented with Swagger, and is covered by xUnit tests that run in GitHub Actions.

## Setup and run instructions (API)

**Requirements:** Visual Studio with the ASP.NET workload, the .NET 10 SDK, SQL Server Express and SSMS.

1. Clone the repository. Part 2 is on the `part2-api` branch, and the complete project (Parts 1 to 3) is on `part3-mvc` and `main` once merged.
2. Open `RaceDay.Api/RaceDay.slnx` in Visual Studio.
3. Open `RaceDay.Api/RaceDay/appsettings.json` and change `Server=` in the `DefaultConnection` string to your own SQL Server instance (for example `.\SQLEXPRESS`).
4. Create the database: open **Tools > NuGet Package Manager > Package Manager Console**, set **Default project** to `RaceDay`, and run `Update-Database`. This creates the `RaceDayDB_Api` database and seeds the two roles (Organiser and Participant).
5. Run the `RaceDay` project (F5). Swagger opens at `https://localhost:7209/swagger`.
6. Log in through `POST /api/auth/login` first. The session cookie it sets is used by every other endpoint.
7. Run the unit tests with **Test > Run All Tests**, or `dotnet test RaceDay.Api/RaceDay.Tests/RaceDay.Tests.csproj`.

Users are created through registration (`POST /api/auth/register` or the MVC Register page). To try everything, register one Organiser and one Participant.

## Authentication and role-based access

- **Registration:** users choose their role (Organiser or Participant) when they register. Any other role is rejected with 400.
- **Password storage:** passwords are hashed with BCrypt before they are stored. The original password is never saved.
- **Sessions:** logging in stores the user's ID and role in the server session (cookie `.RaceDay.Session`, 60 minutes idle). Logging out clears the session.
- **Authorisation:** the `[RequireLogin]` attribute protects every endpoint except register and login. It returns **401** when there is no session and **403** when the role does not match (for example `[RequireLogin("Organiser")]`).
- **Ownership:** an Organiser can only edit or delete their own events and categories, capture results for their own events, upload banners for their own events and view enrolments for their own events. Otherwise the API returns 403.

## Endpoints

| Method | Route | Role | Success |
|---|---|---|---|
| POST | /api/auth/register | Public | 201 |
| POST | /api/auth/login | Public | 200 |
| POST | /api/auth/logout | Any | 200 |
| GET | /api/users/me | Any | 200 |
| PUT | /api/users/me | Any | 200 |
| POST | /api/users/me/picture | Any | 200 |
| GET | /api/events | Any | 200 |
| GET | /api/events/mine | Organiser | 200 |
| GET | /api/events/{id} | Any | 200 |
| POST | /api/events | Organiser | 201 |
| PUT | /api/events/{id} | Organiser (owner) | 200 |
| DELETE | /api/events/{id} | Organiser (owner) | 204 |
| POST | /api/events/{id}/banner | Organiser (owner) | 200 |
| GET | /api/events/{eventId}/categories | Any | 200 |
| POST | /api/events/{eventId}/categories | Organiser (owner) | 201 |
| PUT | /api/categories/{id} | Organiser (owner) | 200 |
| DELETE | /api/categories/{id} | Organiser (owner) | 204 |
| POST | /api/events/{eventId}/enrolments | Participant | 201 |
| GET | /api/users/me/enrolments | Participant | 200 |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) | 200 |
| PUT | /api/enrolments/{id}/status | Organiser (owner) | 200 |
| POST | /api/enrolments/{enrolmentId}/result | Organiser (owner) | 201 |
| GET | /api/users/me/results | Participant | 200 |
| GET | /api/events/{eventId}/results | Any | 200 |

Swagger UI shows each endpoint's purpose, the data it expects and its responses.

## Unit tests

The tests use xUnit and the EF Core in-memory provider with a fake session, so they do not need SQL Server.

| Test class | What it covers |
|---|---|
| `AuthTests` | Registration (success, duplicate email, invalid role), login (success, wrong password, unknown email), logout |
| `AuthorizationTests` | 401 when not logged in, 403 for the wrong role, and that every role-restricted endpoint carries the correct role |
| `EventTests` | Organiser create, update, delete, ownership checks, enrolment conflict and upcoming events only |
| `EnrolmentTests` | Participant enrols and it is recorded as Pending, duplicate, wrong category, past event, own enrolments only, Organiser view and confirm |


## Part 2 video walkthrough

[Watch the Part 2 walkthrough on YouTube](https://youtu.be/qx-lpoOnieA)

---

# Part 3 - MVC Web Application, Azure Blob Storage and Docker

The MVC application is built with ASP.NET Core MVC (.NET 10) and Bootstrap 5. It gives each role its own interface and gets **all** of its data from the Part 2 API. The MVC project has no database access and no copied business rules: every page loads or saves data through the API, and the API makes the final decision on who is allowed to do what.

## How the MVC app works

- **API access:** a typed `ApiClient` (`Services/ApiClient.cs`) sends every request to the API. API failures become friendly messages, so users never see raw error text or a crash.
- **Sessions:** after a successful login the MVC app keeps the user's ID, name and role in its own session (cookie `.RaceDay.Web.Session`) and stores the API's session cookie so it can forward it on every call. Logging out clears both.
- **Role-based navigation:** the layout shows a different menu for Organisers, Participants and visitors. The `[RequireRole]` filter protects every page: visitors who are not logged in are sent to the login page, and the wrong role gets an "access denied" page.
- **Azure Blob Storage:** the MVC app never talks to Azure. Images go from the browser to MVC, then to the API (`POST /api/events/{id}/banner` and `POST /api/users/me/picture`), and the API stores them in Azure Blob Storage.

## Pages

| Role | Pages |
|---|---|
| Visitor | Landing page `/`, Register `/Account/Register`, Log in `/Account/Login` |
| Organiser | Dashboard `/Dashboard`, Events `/ManageEvents` (create, edit, delete, banner upload), Categories `/ManageCategories`, Enrolments `/ManageEnrolments` (confirm entries), Results `/ManageResults` |
| Participant | Upcoming events with filters `/Events`, Event details and enrolment `/Events/Details/{id}`, My Enrolments `/MyEnrolments`, My Results `/MyResults` |
| Both roles | My profile `/Account/Profile` (update details and upload a profile picture), Log out |

Enrolment statuses are colour coded: **green for Confirmed, amber for Pending**. Results are shown as, for example, "47th out of 312 finishers".

## Azure Blob Storage setup

1. In the Azure portal, create a **Storage account**.
2. Under **Settings > Configuration**, set **Allow Blob anonymous access** to **Enabled** and save.
3. Under **Data storage > Containers**, create a container named `raceday-images` with **Anonymous access level: Blob**. Images can then be shown on the site, but no one can list or change the container.
4. Under **Security + networking > Access keys**, copy the **Connection string**.
5. Give the connection string to the API as `AzureStorage:ConnectionString`:
   - In Visual Studio: right-click the `RaceDay` API project > **Manage User Secrets** and paste
     `{ "AzureStorage": { "ConnectionString": "YOUR_CONNECTION_STRING" } }`
   - Or set the environment variable `AzureStorage__ConnectionString`.

The connection string is never committed to the repository. The API accepts JPG, PNG and WEBP images up to 5 MB, checks the real file signature and replaces (and deletes) the old image when a new one is uploaded.

## Run locally (without Docker)

1. Complete the API setup above (database, connection string, Azure Storage).
2. In Visual Studio right-click the solution > **Configure Startup Projects** > **Multiple startup projects** and set both `RaceDay` (API) and `RaceDay.Web` to **Start**.
3. Check `RaceDay.Web/appsettings.json`: `ApiSettings:BaseUrl` must match the API's HTTPS address (default `https://localhost:7209/`).
4. Press F5. The MVC site opens in the browser and the API's Swagger page opens beside it.
5. Register one Organiser and one Participant to try both interfaces.

## Run with Docker

The Dockerfile and compose file containerise the **MVC application**. The API (and its SQL Server database) keeps running on the host machine, and the container reaches it at `host.docker.internal`.

**Requirements:** Docker (Docker Desktop with Linux containers, or Docker Engine on Linux), and the API running on the host with its `https` launch profile (`https://localhost:7209`).

**Single command** (run from the repository root):

```
docker compose up --build
```

Then open **http://localhost:8080**. Stop it with `Ctrl+C` or `docker compose down`.

The same thing without Compose:

```
docker build -t raceday-web ./RaceDay.Web
docker run --rm -p 8080:8080 -e ApiSettings__BaseUrl=https://host.docker.internal:7209/ -e ApiSettings__TrustDevCertificate=true --name raceday-web raceday-web
```

If the container cannot reach the API, open `RaceDay.Api/RaceDay/Properties/launchSettings.json`, set the `https` profile's `applicationUrl` to `https://*:7209;http://*:5102`, restart the API, and allow it through the Windows firewall on private networks.

**Dockerfile decisions**
- **Multi-stage build:** the .NET SDK image compiles and publishes the app, and the final image contains only the smaller ASP.NET runtime image.
- **Restore before copy:** the project file is copied and restored first, so rebuilds reuse the cached packages layer.
- **`.dockerignore`:** keeps `bin`, `obj` and Git files out of the build.
- **Non-root user:** the app runs without administrator rights inside the container.
- **Configuration through environment variables:** the API address comes from `ApiSettings__BaseUrl`, so the same image runs anywhere.
- **Same behaviour as local:** the app uses the same session, API calls and invariant-culture number handling in and out of the container. Inside the container it listens on HTTP port 8080, so HTTPS redirection is skipped there.

## UI design

The interface uses a sporting navy-and-orange RaceDay theme with a condensed italic heading font, Bootstrap 5 for the responsive layout, one shared layout, and the same colours, spacing and components on every page. Invalid input is handled with clear validation messages, and API errors are turned into friendly messages.

## Differences from the earlier plans

The following changes were made deliberately and are explained here as the paper requires. Part 1's SQL script matches the ERD exactly, and the API builds the same schema (tables, columns, types, defaults, check constraints and unique rules) in a database called `RaceDayDB_Api`. EF Core generates its own index and constraint names (for example `IX_Users_Email` instead of the `UQ_` names in the Part 1 script).

- `POST /api/events/{id}/banner` was not built in Part 2 because it needs Azure Blob Storage. It is implemented in Part 3.
- `POST /api/users/me/picture` was added for profile pictures (the Part 1 plan only had `PUT /api/users/me`, which takes a picture URL).
- `GET /api/events/mine` was added so the Organiser dashboard can list all of their events, including past ones.
- `GET /api/events` accepts optional `eventType` and `search` filters for the Participant home page.
- `PUT /api/enrolments/{id}/status` was added so an Organiser can move an enrolment from Pending to Confirmed. The Part 1 plan had no way to do this.
- `DELETE /api/events/{id}` and `DELETE /api/categories/{id}` return 409 when enrolments exist, because the database blocks those deletes.
- `POST /api/events/{eventId}/enrolments` also returns 400 for events that have already taken place, and 403 for Organisers.
- `POST /api/enrolments/{enrolmentId}/result` also returns 400 for an invalid finish time or position, and 409 if the enrolment already has a result.
- Every result includes `totalFinishers`, so the MVC app can show "47th out of 312 finishers".
- The API does not check that an event has finished before accepting its results, and the Results page only shows a notice for events that have not taken place yet.
- Participants cannot withdraw an enrolment, because the Part 1 plan has no endpoint for it.
- When a logged-in user opens the landing page they are sent straight to their own start page (events for Participants, dashboard for Organisers).
- `TrustDevCertificate` (default `true` in `appsettings.json`) makes the MVC app accept the API's self-signed development certificate. It is meant for local development only and should be `false` against a real API.

## Part 3 CI/CD

The workflow `.github/workflows/part3-ci.yml` runs three jobs on every push:
1. **Build MVC project:** restores and builds `RaceDay.Web`.
2. **Build and run the Docker container:** builds the image, starts the container, checks that the home and login pages are served, and checks that a protected page redirects a logged-out visitor to the login page.
3. **Build API and run unit tests:** the Part 2 test job.

![Part 3 CI green build](docs/Part3_CI_GreenBuild.png)

## Part 3 video walkthrough

[Watch the Part 3 walkthrough on YouTube](https://youtu.be/tNC6-VV58WQ)
