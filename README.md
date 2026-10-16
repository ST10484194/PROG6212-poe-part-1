# RaceDay - Event Management System

**RaceDay** is a full-stack web-based event management system built for the South African road running, walking, and cycling community. It allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse events, enter them, and track their personal race history.

This is an individual Portfolio of Evidence built in three parts:

| Part | Description | Location |
|---|---|---|
| Part 1 | Planning and database: ERD, endpoint plan, SQL script | `docs/` |
| Part 2 | RESTful ASP.NET Core Web API with EF Core, session authentication, Swagger and unit tests | `RaceDay.Api/` (branch `part2-api`) |
| Part 3 | MVC web application, Azure Blob Storage, Docker | Not started |

## Roles

- **Organiser** - can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
- **Participant** - can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

## Repository Structure

```
docs/
├── RaceDay_ERD.png                 Entity Relationship Diagram (Part 1)
├── RaceDay_Endpoint_Plan.md        Full API endpoint plan (Part 1)
├── RaceDay_Database.sql            Database creation and seed script (Part 1)
└── Part2_CI_GreenBuild.png         Part 2 CI screenshot
RaceDay.Api/
├── RaceDay/                        ASP.NET Core Web API (Part 2)
│   ├── Auth/                       RequireLogin attribute (session and role check)
│   ├── Controllers/                Auth, Users, Events, Categories, Enrolments, Results
│   ├── Data/                       AppDbContext (EF Core, matches the Part 1 schema)
│   ├── Dtos/                       Request and response classes
│   ├── Migrations/                 EF Core code-first migrations
│   ├── Models/                     Entity classes
│   └── Program.cs
├── RaceDay.Tests/                  xUnit unit tests
└── RaceDay.slnx
.github/workflows/
├── part1-validate.yml              Validates the Part 1 repository structure
└── part2-build-test.yml            Builds the API and runs all unit tests
```

---

# Part 1 - Planning and Database

Part 1 contains the system planning: an Entity Relationship Diagram (ERD), a full API endpoint plan, and a SQL database script.

**Part 1 build status:** ✅ Passing

<img width="1431" height="450" alt="Screenshot 2026-09-04 134646" src="https://github.com/user-attachments/assets/afc33081-7a55-49b4-9430-7f6d7f7d93bc" />

### Part 1 video walkthrough

[Watch the Part 1 walkthrough on YouTube](https://youtu.be/PVn1d24QPz0)

---

# Part 2 - RESTful API

The API is built with ASP.NET Core Web API (C#), Entity Framework Core (code-first) and SQL Server. It follows the endpoint plan from Part 1, uses session-based authentication with two roles, is documented with Swagger, and is covered by xUnit tests that run in GitHub Actions.

## Setup and run instructions

**Requirements:** Visual Studio 2022 with the ASP.NET workload, the .NET 8 SDK, SQL Server Express and SSMS.

1. Clone the repository and switch to the `part2-api` branch.
2. Open `RaceDay.Api/RaceDay.slnx` in Visual Studio.
3. Open `RaceDay.Api/RaceDay/appsettings.json` and change `Server=` in the `DefaultConnection` string to your own SQL Server instance (for example `.\SQLEXPRESS`).
4. Create the database: open **Tools > NuGet Package Manager > Package Manager Console**, set **Default project** to `RaceDay`, and run `Update-Database`. This creates the `RaceDayDB_Api` database and seeds the two roles (Organiser and Participant).
5. Press **F5**. Swagger opens at `https://localhost:7209/swagger`.
6. Log in through `POST /api/auth/login` first. The session cookie it sets is used by every other endpoint.
7. Run the unit tests with **Test > Run All Tests**, or `dotnet test RaceDay.Api/RaceDay.Tests/RaceDay.Tests.csproj`.

## Authentication and role-based access

- **Registration:** users choose their role (Organiser or Participant) when they register. Any other role is rejected with 400.
- **Password storage:** passwords are hashed with BCrypt before they are stored. The original password is never saved.
- **Sessions:** logging in stores the user's ID and role in the server session (cookie `.RaceDay.Session`, 60 minutes idle). Logging out clears the session.
- **Authorisation:** the `[RequireLogin]` attribute protects every endpoint except register and login. It returns **401** when there is no session, and **403** when the role does not match (for example `[RequireLogin("Organiser")]`).
- **Ownership:** an Organiser can only edit or delete their own events and categories, capture results for their own events and view enrolments for their own events. Otherwise the API returns 403.

## Endpoints

| Method | Route | Role | Success |
|---|---|---|---|
| POST | /api/auth/register | Public | 201 |
| POST | /api/auth/login | Public | 200 |
| POST | /api/auth/logout | Any | 200 |
| GET | /api/users/me | Any | 200 |
| PUT | /api/users/me | Any | 200 |
| GET | /api/events | Any | 200 |
| GET | /api/events/mine | Organiser | 200 |
| GET | /api/events/{id} | Any | 200 |
| POST | /api/events | Organiser | 201 |
| PUT | /api/events/{id} | Organiser (owner) | 200 |
| DELETE | /api/events/{id} | Organiser (owner) | 204 |
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

## Deliberate differences from the Part 1 plan

- `POST /api/events/{id}/banner` is not built in Part 2. It needs Azure Blob Storage, which is Part 3.
- `GET /api/events/mine` was added so the Organiser dashboard can list all of their events, including past ones.
- `GET /api/events` accepts optional `eventType` and `search` filters for the Participant home page.
- `PUT /api/enrolments/{id}/status` was added so an Organiser can move an enrolment from Pending to Confirmed. The Part 1 plan had no way to do this.
- `DELETE /api/events/{id}` and `DELETE /api/categories/{id}` return 409 when enrolments exist, because the database blocks those deletes.
- `POST /api/events/{eventId}/enrolments` also returns 400 for events that have already taken place, and 403 for Organisers.
- `POST /api/enrolments/{enrolmentId}/result` also returns 400 for an invalid finish time or position, and 409 if the enrolment already has a result.
- Every result includes `totalFinishers`, so Part 3 can show "47th out of 312 finishers".
- The API does not check that an event has finished before accepting its results.
- The EF Core database is called `RaceDayDB_Api`. It has the same six tables, columns, types, defaults, Pending/Confirmed and Run/Walk/Cycle checks and unique rules as the Part 1 script. EF Core generates its own index and constraint names (for example `IX_Users_Email` instead of the `UQ_` names in the Part 1 script).

## Unit tests

The tests use xUnit and the EF Core in-memory provider with a fake session, so they do not need SQL Server.

| Test class | What it covers |
|---|---|
| `AuthTests` | Registration (success, duplicate email, invalid role), login (success, wrong password, unknown email), logout |
| `AuthorizationTests` | 401 when not logged in, 403 for the wrong role, and that every role-restricted endpoint carries the correct role |
| `EventTests` | Organiser create, update, delete, ownership checks, enrolment conflict and upcoming events only |
| `EnrolmentTests` | Participant enrols and it is recorded as Pending, duplicate, wrong category, past event, own enrolments only, Organiser view and confirm |

## CI/CD

The workflow `.github/workflows/part2-build-test.yml` builds the API and runs all unit tests on every push.

**Latest build status:** ✅ Passing

<img width="1427" height="242" alt="Screenshot 2026-10-16 175449" src="https://github.com/user-attachments/assets/944eda64-8b8d-466c-8a15-ad32df937d13" />


## Part 2 video walkthrough

[Watch the Part 2 walkthrough on YouTube](PASTE_YOUR_PART_2_YOUTUBE_LINK_HERE)
