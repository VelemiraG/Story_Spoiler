using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using StorySpoilerExam;

namespace StorySpoilerExam
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient client;
        private static string createdStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net/api";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("velemirag", "123456");
            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString();
        }

        [Test, Order(1)]
        public void CreateNewStorySpoiler_ShouldReturn201AndStoryId()
        {
            var story = new
            {
                Title = "My Secret Ending",
                Description = "The hero dies.",
                Url = ""
            };

            var request = new RestRequest("/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createdStoryId = json.GetProperty("storyId").GetString();

            Console.WriteLine("Created Story ID: " + createdStoryId);

            Assert.That(createdStoryId, Is.Not.Null.Or.Empty);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully created!"));
            Console.WriteLine("RESPONSE FROM CREATE:");
            Console.WriteLine(response.Content);


        }


        [Test, Order(2)]
        public void EditCreatedStorySpoiler_ShouldReturn200AndSuccessMessage()
        {
            Assert.That(createdStoryId, Is.Not.Null.Or.Empty, "StoryId should not be null before PUT");
            Console.WriteLine("Editing story ID: " + createdStoryId);

            var updatedStory = new
            {
                Title = "Edited Spoiler Title",
                Description = "Now the villain dies!",
                Url = ""
            };

            var request = new RestRequest($"/Story/Edit/{createdStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStorySpoilers_ShouldReturn200AndNonEmptyList()
        {
            var request = new RestRequest("/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Null.Or.Empty, "Response content should not be null or empty");

            var stories = JsonSerializer.Deserialize<List<object>>(response.Content);
            Assert.That(stories, Is.Not.Null.And.Not.Empty, "Expected non-empty list of stories");
        }

        [Test, Order(4)]
        public void DeleteCreatedStorySpoiler_ShouldReturn200AndSuccessMessage()
        {
            Assert.That(createdStoryId, Is.Not.Null.Or.Empty, "StoryId should not be null before DELETE");

            var request = new RestRequest($"/Story/Delete/{createdStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Is.Not.Null.Or.Empty, "Response content should not be null or empty");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var msg = json.GetProperty("msg").GetString();

            Assert.That(msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStorySpoiler_WithMissingFields_ShouldReturn400()
        {
            var story = new 
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStorySpoiler_ShouldReturn404()
        {
            var fakeId = "00000000-0000-0000-0000-000000000000";

            var updatedStory = new
            {
                Title = "Fake Title",
                Description = "This shouldn't exist.",
                Url = ""
            };

            var request = new RestRequest($"/Story/Edit/{fakeId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStorySpoiler_ShouldReturn400()
        {
            var fakeId = "00000000-0000-0000-0000-000000000000";

            var request = new RestRequest($"/Story/Delete/{fakeId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }



        [OneTimeTearDown]
        public void CleanUp()
        {
            client?.Dispose();
        }
    }
}
