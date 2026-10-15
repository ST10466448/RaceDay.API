using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace RaceDay.API.Test
{
    public class UnitTest1
    {
        private static RaceDayDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

            return new RaceDayDbContext(options);
        }

        private static IConfiguration CreateConfiguration()
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] =
                    "RaceDayTestSigningKey-AtLeast32Characters!",
                ["Jwt:Issuer"] = "RaceDay.API",
                ["Jwt:Audience"] = "RaceDay.API.Users",
                ["Jwt:ExpiryMinutes"] = "60"
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        [Fact]
        public async Task Register_ValidParticipant_ReturnsCreated()
        {
            using var context = CreateContext();
            var controller = new AuthController(
                context, CreateConfiguration());

            var request = new RegisterRequest
            {
                FullName = "Test Participant",
                Email = "test@example.com",
                Password = "TestPassword123!",
                Role = "Participant"
            };

            var result = await controller.Register(request);

            var created = Assert.IsType<CreatedResult>(result);
            Assert.Equal(1, context.Users.Count());
            Assert.Equal("Participant", context.Users.Single().Role);
            Assert.NotEqual(
                "TestPassword123!",
                context.Users.Single().PasswordHash);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsJwtToken()
        {
            using var context = CreateContext();
            var controller = new AuthController(
                context, CreateConfiguration());

            var registerRequest = new RegisterRequest
            {
                FullName = "Test Participant",
                Email = "login@example.com",
                Password = "TestPassword123!",
                Role = "Participant"
            };

            await controller.Register(registerRequest);

            var loginRequest = new LoginRequest
            {
                Email = "login@example.com",
                Password = "TestPassword123!"
            };

            var result = await controller.Login(loginRequest);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            var json = System.Text.Json.JsonSerializer
                .Serialize(ok.Value);

            using var document =
                System.Text.Json.JsonDocument.Parse(json);

            var token = document.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            var jwt = new JwtSecurityTokenHandler()
                .ReadJwtToken(token);

            Assert.Equal("RaceDay.API", jwt.Issuer);
        }
        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            using var context = CreateContext();
            var controller = new AuthController(
                context, CreateConfiguration());

            var request = new RegisterRequest
            {
                FullName = "Test Participant",
                Email = "duplicate@example.com",
                Password = "TestPassword123!",
                Role = "Participant"
            };

            await controller.Register(request);

            var result = await controller.Register(request);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(1, context.Users.Count());
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var controller = new AuthController(
                context, CreateConfiguration());

            var registerRequest = new RegisterRequest
            {
                FullName = "Test Participant",
                Email = "wrongpassword@example.com",
                Password = "CorrectPassword123!",
                Role = "Participant"
            };

            await controller.Register(registerRequest);

            var loginRequest = new LoginRequest
            {
                Email = "wrongpassword@example.com",
                Password = "IncorrectPassword123!"
            };

            var result = await controller.Login(loginRequest);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
        [Fact]
        public async Task CreateEvent_ValidRoute_ReturnsCreated()
        {
            using var context = CreateContext();

            var route = new RaceDay.API.Models.Route
            {
                RouteName = "Test Route",
                Description = "Route for unit testing",
                DistanceKM = 5,
                StartPoint = "Start Point",
                FinishPoint = "Finish Point"
            };

            context.Routes.Add(route);
            await context.SaveChangesAsync();

            var configuration = CreateConfiguration();
            var controller = new RaceDay.API.Controllers.EventsController(
                context);

            var claims = new[]
            {
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier, "1"),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role, "Organiser")
            };

            controller.ControllerContext =
                new Microsoft.AspNetCore.Mvc.ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(
                            new System.Security.Claims.ClaimsIdentity(
                                claims, "Test"))
                    }
                };

            var raceEvent = new RaceDay.API.Models.Event
            {
                OrganizerID = 999,
                RouteID = route.RouteID,
                EventName = "Test Running Event",
                EventDate = DateTime.UtcNow.Date.AddDays(7),
                StartTime = TimeSpan.FromHours(8),
                Venue = "Test Venue",
                WeatherLocation = "Cape Town"
            };

            var result = await controller.CreateEvent(raceEvent);

            var created = Assert.IsType<
                Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(result.Result);

            Assert.Equal(1, context.Events.Count());
            Assert.Equal(1, context.Events.Single().OrganizerID);
            Assert.Equal("Test Running Event",
                context.Events.Single().EventName);
        }

        [Fact]
        public async Task CreateEvent_InvalidRoute_ReturnsBadRequest()
        {
            using var context = CreateContext();

            var controller = new RaceDay.API.Controllers.EventsController(
                context);

            var claims = new[]
            {
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier, "1"),
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role, "Organiser")
            };

            controller.ControllerContext =
                new Microsoft.AspNetCore.Mvc.ControllerContext
                {
                    HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                    {
                        User = new System.Security.Claims.ClaimsPrincipal(
                            new System.Security.Claims.ClaimsIdentity(
                                claims, "Test"))
                    }
                };

            var raceEvent = new RaceDay.API.Models.Event
            {
                OrganizerID = 1,
                RouteID = 999,
                EventName = "Invalid Route Event",
                EventDate = DateTime.UtcNow.Date.AddDays(7),
                StartTime = TimeSpan.FromHours(8),
                Venue = "Test Venue",
                WeatherLocation = "Cape Town"
            };

            var result = await controller.CreateEvent(raceEvent);

            Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(
                result.Result);

            Assert.Empty(context.Events);
        }
        [Fact]
        public async Task GetEvents_WithoutToken_ReturnsUnauthorized()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/Events");

            Assert.Equal(
                System.Net.HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
        [Fact]
        public async Task GetEvents_WithParticipantToken_ReturnsSuccess()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var registerData = new
            {
                fullName = "Integration Test Participant",
                email = "integration.participant@example.com",
                password = "TestPassword123!",
                role = "Participant"
            };

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register", registerData);

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            var loginData = new
            {
                email = "integration.participant@example.com",
                password = "TestPassword123!"
            };

            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login", loginData);



            var loginBody = await loginResponse.Content.ReadAsStringAsync();

            Assert.True(
                loginResponse.IsSuccessStatusCode,
                $"Login failed. Status: {(int)loginResponse.StatusCode}. " +
                $"Response: {loginBody}");

            using var loginJson =
                System.Text.Json.JsonDocument.Parse(loginBody);

            var token = loginJson.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var eventsResponse = await client.GetAsync("/api/Events");

            var authError = eventsResponse.Headers
     .WwwAuthenticate?.ToString() ?? "No authentication details";

            var responseBody = await eventsResponse.Content
                .ReadAsStringAsync();

            Assert.True(
                eventsResponse.IsSuccessStatusCode,
                $"Status: {(int)eventsResponse.StatusCode}. " +
                $"Authentication details: {authError}. " +
                $"Response: {responseBody}");
        }
        [Fact]
        public async Task CreateEvent_WithoutToken_ReturnsUnauthorized()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var eventData = new
            {
                eventName = "Test Race",
                eventDate = DateTime.UtcNow.AddDays(30),
                startTime = "09:00:00",
                venue = "Cape Town",
                weatherLocation = "Cape Town",
                routeID = 1
            };

            var response = await client.PostAsJsonAsync(
                "/api/Events", eventData);

            Assert.Equal(
                System.Net.HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
        [Fact]
        public async Task CreateEvent_WithParticipantToken_ReturnsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var registerData = new
            {
                fullName = "Event Test Participant",
                email = "event.test.participant@example.com",
                password = "TestPassword123!",
                role = "Participant"
            };

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register", registerData);

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            var loginData = new
            {
                email = "event.test.participant@example.com",
                password = "TestPassword123!"
            };

            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login", loginData);

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                loginResponse.StatusCode);

            using var loginJson = System.Text.Json.JsonDocument.Parse(
                await loginResponse.Content.ReadAsStringAsync());

            var token = loginJson.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var eventData = new
            {
                eventName = "Test Race",
                eventDate = DateTime.UtcNow.AddDays(30),
                startTime = "09:00:00",
                venue = "Cape Town",
                weatherLocation = "Cape Town",
                routeID = 1
            };

            var response = await client.PostAsJsonAsync(
                "/api/Events", eventData);

            Assert.Equal(
                System.Net.HttpStatusCode.Forbidden,
                response.StatusCode);
        }
        [Fact]
        public async Task GetEvents_WithOrganiserToken_ReturnsSuccess()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var email = $"organiser.{Guid.NewGuid()}@example.com";

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    fullName = "Test Organiser",
                    email = email,
                    password = "TestPassword123!",
                    role = "Participant"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            // Change the role directly in the test database.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<RaceDayDbContext>();

                var user = await db.Users
                    .FirstAsync(u => u.Email == email);

                user.Role = "Organiser";
                await db.SaveChangesAsync();
            }

            // Log in to receive a token containing the Organiser role.
            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = "TestPassword123!"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                loginResponse.StatusCode);

            using var loginJson = System.Text.Json.JsonDocument.Parse(
                await loginResponse.Content.ReadAsStringAsync());

            var token = loginJson.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var eventsResponse = await client.GetAsync("/api/Events");

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                eventsResponse.StatusCode);
        }
        [Fact]
        public async Task CreateCategory_WithParticipantToken_ReturnsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var email = $"category.{Guid.NewGuid()}@example.com";

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    fullName = "Category Test Participant",
                    email = email,
                    password = "TestPassword123!",
                    role = "Participant"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = "TestPassword123!"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                loginResponse.StatusCode);

            using var loginJson = System.Text.Json.JsonDocument.Parse(
                await loginResponse.Content.ReadAsStringAsync());

            var token = loginJson.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var categoryData = new
            {
                categoryName = "Test Category",
                eventID = 1
            };

            var response = await client.PostAsJsonAsync(
                "/api/EventCategories", categoryData);

            Assert.Equal(
                System.Net.HttpStatusCode.Forbidden,
                response.StatusCode);
        }
        [Fact]
        public async Task CreateEntry_WithOrganiserToken_ReturnsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var email = $"organiser.entry.{Guid.NewGuid()}@example.com";

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    fullName = "Entry Test Organiser",
                    email = email,
                    password = "TestPassword123!",
                    role = "Participant"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            // Give this test user the Organiser role in the test database.
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<RaceDayDbContext>();

                var user = await db.Users
                    .FirstAsync(u => u.Email == email);

                user.Role = "Organiser";
                await db.SaveChangesAsync();
            }

            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = "TestPassword123!"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                loginResponse.StatusCode);

            using var loginJson = System.Text.Json.JsonDocument.Parse(
                await loginResponse.Content.ReadAsStringAsync());

            var token = loginJson.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var entryData = new
            {
                eventID = 1,
                categoryID = 1,
                raceNumber = 999
            };

            var response = await client.PostAsJsonAsync(
                "/api/Entries", entryData);

            Assert.Equal(
                System.Net.HttpStatusCode.Forbidden,
                response.StatusCode);
        }
        [Fact]
        public async Task CreateResult_WithParticipantToken_ReturnsForbidden()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var email = $"result.{Guid.NewGuid()}@example.com";

            var registerResponse = await client.PostAsJsonAsync(
                "/api/Auth/register",
                new
                {
                    fullName = "Result Test Participant",
                    email = email,
                    password = "TestPassword123!",
                    role = "Participant"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            var loginResponse = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    email = email,
                    password = "TestPassword123!"
                });

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                loginResponse.StatusCode);

            using var loginJson = System.Text.Json.JsonDocument.Parse(
                await loginResponse.Content.ReadAsStringAsync());

            var token = loginJson.RootElement
                .GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var resultData = new
            {
                finishTime = "00:30:00",
                position = 1,
                averagePace = "05:00",
                resultDate = DateTime.UtcNow
            };

            var response = await client.PostAsJsonAsync(
                "/api/Results", resultData);

            Assert.Equal(
                System.Net.HttpStatusCode.Forbidden,
                response.StatusCode);
        }
        [Fact]
        public async Task GetResult_WithParticipantToken_CannotAccessAnotherParticipantsResult()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            // Register the first participant
            var firstEmail = $"first.{Guid.NewGuid()}@example.com";
            var firstRegister = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                fullName = "First Participant",
                email = firstEmail,
                password = "RaceDayTest2026!",
                role = "Participant"
            });
            Assert.Equal(System.Net.HttpStatusCode.Created, firstRegister.StatusCode);

            // Register the second participant
            var secondEmail = $"second.{Guid.NewGuid()}@example.com";
            var secondRegister = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                fullName = "Second Participant",
                email = secondEmail,
                password = "RaceDayTest2026!",
                role = "Participant"
            });
            Assert.Equal(System.Net.HttpStatusCode.Created, secondRegister.StatusCode);

            // Log in as the first participant
            var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new
            {
                email = firstEmail,
                password = "RaceDayTest2026!"
            });
            Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);

            var loginJson = await loginResponse.Content.ReadAsStringAsync();
            using var loginDocument = System.Text.Json.JsonDocument.Parse(loginJson);
            var token = loginDocument.RootElement.GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            // Retrieve both participants from the test database
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RaceDay.API.Data.RaceDayDbContext>();

            var firstParticipant = await db.Users.FirstAsync(u => u.Email == firstEmail);
            var secondParticipant = await db.Users.FirstAsync(u => u.Email == secondEmail);

            // Create a route, event and category for the test entries
            var route = new RaceDay.API.Models.Route
            {
                RouteName = "Test Route",
                Description = "Route created for the results ownership test",
                DistanceKM = 5.0m,
                StartPoint = "Start Point",
                FinishPoint = "Finish Point"
            };

            db.Routes.Add(route);
            await db.SaveChangesAsync();

            var testEvent = new RaceDay.API.Models.Event
            {
                OrganizerID = firstParticipant.UserID,
                RouteID = route.RouteID,
                EventName = "Test Race",
                EventDate = DateTime.UtcNow.Date.AddDays(7),
                StartTime = TimeSpan.FromHours(8),
                Venue = "Test Venue",
                WeatherLocation = "Cape Town"
            };

            db.Events.Add(testEvent);
            await db.SaveChangesAsync();

            var category = new RaceDay.API.Models.EventCategory
            {
                EventID = testEvent.EventID,
                CategoryName = "5 KM Open",
                DistanceKM = 5.0m,
                EntryFee = 100.00m,
                MaxParticipants = 100
            };

            db.EventCategories.Add(category);
            await db.SaveChangesAsync();

            // Create an entry and result for the second participant
            var secondEntry = new RaceDay.API.Models.Entry
            {
                ParticipantID = secondParticipant.UserID,
                CategoryID = category.CategoryID,
                EntryStatus = "Confirmed"
            };

            db.Entries.Add(secondEntry);
            await db.SaveChangesAsync();

            var secondResult = new RaceDay.API.Models.Result
            {
                EntryID = secondEntry.EntryID,
                FinishTime = TimeSpan.FromMinutes(30),
                Position = 1,
                AveragePace = 5.0m
            };

            db.Results.Add(secondResult);
            await db.SaveChangesAsync();

            // Request the second participant's result while logged in as the first
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/Results/{secondResult.ResultID}");

            // The API should not reveal another participant's result
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
        [Fact]
        public async Task GetResults_WithParticipantToken_ReturnsOnlyOwnResults()
        {
            using var factory = new CustomWebApplicationFactory();
            using var client = factory.CreateClient();

            var email = $"participant.{Guid.NewGuid()}@example.com";

            var registerResponse = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                fullName = "Results Test Participant",
                email = email,
                password = "RaceDayTest2026!",
                role = "Participant"
            });

            Assert.Equal(
                System.Net.HttpStatusCode.Created,
                registerResponse.StatusCode);

            var loginResponse = await client.PostAsJsonAsync("/api/Auth/login", new
            {
                email = email,
                password = "RaceDayTest2026!"
            });

            Assert.Equal(
                System.Net.HttpStatusCode.OK,
                loginResponse.StatusCode);

            var loginJson = await loginResponse.Content.ReadAsStringAsync();
            using var document = System.Text.Json.JsonDocument.Parse(loginJson);
            var token = document.RootElement.GetProperty("token").GetString();

            Assert.False(string.IsNullOrWhiteSpace(token));

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", token);

            var response = await client.GetAsync("/api/Results");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

            var results = await response.Content.ReadFromJsonAsync<
                List<RaceDay.API.Models.Result>>();

            Assert.NotNull(results);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<
                RaceDay.API.Data.RaceDayDbContext>();

            var participant = await db.Users.FirstAsync(u => u.Email == email);

            var ownEntryIds = await db.Entries
                .Where(e => e.ParticipantID == participant.UserID)
                .Select(e => e.EntryID)
                .ToListAsync();

            Assert.All(results, result =>
                Assert.Contains(result.EntryID, ownEntryIds));
        }
    }
}