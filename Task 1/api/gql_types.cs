using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace api
{

    [DynamoDBTable("music")]
    public class Music
    {
        [DynamoDBProperty("title")]
        public string? Title { get; set; }

        [DynamoDBProperty("artist")]
        public string? Artist { get; set; }

        [DynamoDBProperty("year")]
        public short? Year { get; set; }

        [DynamoDBProperty("web_url")]
        public string? WebURL { get; set; }

        [DynamoDBProperty("img_url")]
        public string? ImgURL { get; set; }
    }

    [DynamoDBTable("login")]
    public class User
    {
        [DynamoDBProperty("email")]
        public string? EMail { get; set; }

        [DynamoDBProperty("username")]
        public string? Username { get; set; }

        [DynamoDBProperty("password")]
        public string? Password { get; set; }
    }

    public class Helpers
    {
        public static User UserByEmail(string email)
        {
            AmazonDynamoDBClient dynamoClient = new();
            DynamoDBContext dynamoContext = new(dynamoClient);

            return dynamoContext.LoadAsync<User>(email).Result;
        }
    }

    public class Query
    {
        private const string BUCKET = "cosc2639-2-s3812552";
        private const string MUSIC_FILE = "a2.json";
        private const string MUSIC_TABLE = "music";

        public bool LoadMusic()
        {
            try
            {
                // Set up a new instance of Dynamo and S3 clients
                AmazonS3Client s3Client = new(Amazon.RegionEndpoint.APSoutheast2);
                AmazonDynamoDBClient dynamoClient = new();
                DynamoDBContext dynamoContext = new(dynamoClient);

                // Grab the file
                Stream response = s3Client.GetObjectAsync(
                    new()
                    {
                        BucketName = BUCKET,
                        Key = MUSIC_FILE,
                    }
                ).Result.ResponseStream;

                // Deserialise the JSON as an object so we can iterate
                JObject data = JsonConvert.DeserializeObject<JObject>(
                    new StreamReader(response).ReadToEnd()
                );

                // Loop through the file instances and add to Dynamo and S3
                if (data.ContainsKey("songs"))
                {
                    // If the table doesn't exists, create it
                    if (!dynamoClient.ListTablesAsync().Result.TableNames.Contains(MUSIC_TABLE))
                    {
                        // Recreate the table
                        dynamoClient.CreateTableAsync(new()
                        {
                            TableName = MUSIC_TABLE,
                            AttributeDefinitions = new() {
                                new AttributeDefinition
                                {
                                    AttributeName = "title",
                                    AttributeType = "S"
                                }
                            },
                            KeySchema = new() {
                                new KeySchemaElement
                                {
                                    AttributeName = "title",
                                    KeyType = "HASH"
                                }
                            },
                            ProvisionedThroughput = new ProvisionedThroughput
                            {
                                ReadCapacityUnits = 1,
                                WriteCapacityUnits = 1
                            },
                        }).Wait();

                        // Loop until table is created
                        do
                        {
                            // Wait 5s for the table to be created
                            Thread.Sleep(5000);
                        } while (!dynamoClient.ListTablesAsync().Result.TableNames.Contains(MUSIC_TABLE));
                    }

                    // Retrieve all rows from the table
                    List<ScanCondition> scanConditions = new() { new ScanCondition("Title", ScanOperator.IsNotNull) };
                    List<Music> musicRows = dynamoContext.ScanAsync<Music>(scanConditions).GetRemainingAsync().Result;

                    // Delete the rows
                    foreach (Music music in musicRows)
                    {
                        dynamoContext.DeleteAsync<Music>(music.Title).Wait();
                    }

                    // Loop through the song elements
                    foreach (var song in data["songs"])
                    {
                        // Output song title
                        Console.WriteLine("Downloading: " + song["title"].Value<string>());

                        // Grab the image url
                        string imageUrl = song["img_url"].Value<string>();
                        imageUrl = imageUrl[(imageUrl.LastIndexOf("/") + 1)..];

                        // Add the row to Dynamo
                        dynamoContext.SaveAsync(new Music
                        {
                            Title = song["title"].Value<string>(),
                            Artist = song["artist"].Value<string>(),
                            Year = song["year"].Value<short>(),
                            WebURL = song["web_url"].Value<string>(),
                            ImgURL = imageUrl
                        }).Wait();

                        // Download the file
                        HttpResponseMessage file = new HttpClient().GetAsync(song["img_url"].Value<string>()).Result;

                        // Put it in S3
                        s3Client.PutObjectAsync(
                            new()
                            {
                                BucketName = BUCKET,
                                Key = string.Concat("images/", imageUrl),
                                InputStream = file.Content.ReadAsStream()
                            }
                        ).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return true;
        }

        public User? UserByEmailPassword(string email, string password)
        {
            User user = Helpers.UserByEmail(email);

            if (user != null && user.Password == password)
            {
                return user;
            }

            return null;
        }
    }

    public class Mutation
    {
        public bool RegisterUser(string email, string username, string password)
        {
            AmazonDynamoDBClient dynamoClient = new();
            DynamoDBContext dynamoContext = new(dynamoClient);

            if (Helpers.UserByEmail(email) == null)
            {
                dynamoContext.SaveAsync<User>(new()
                {
                    EMail = email,
                    Username = username,
                    Password = password
                });

                return true;
            }

            return false;
        }
    }

}