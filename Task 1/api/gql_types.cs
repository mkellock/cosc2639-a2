using System.Linq.Expressions;
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
        public int? Year { get; set; }

        [DynamoDBProperty("web_url")]
        public string? WebURL { get; set; }

        [DynamoDBProperty("img_url")]
        public string? ImgURL { get; set; }

        [DynamoDBProperty("title_artist")]
        public string? TitleArtist { get; set; }
    }

    [DynamoDBTable("login")]
    public class User
    {
        [DynamoDBProperty("email_password")]
        public string? EMailPassword { get; set; }

        [DynamoDBProperty("email")]
        public string? EMail { get; set; }

        [DynamoDBProperty("username")]
        public string? Username { get; set; }

        [DynamoDBProperty("password")]
        public string? Password { get; set; }
    }

    [DynamoDBTable("subscription")]
    public class Subscription : Music
    {
        [DynamoDBProperty("email")]
        public string? EMail { get; set; }

        [DynamoDBProperty("email_title_artist")]
        public string? EMailTitleArtist { get; set; }
    }

    public class Helpers
    {
        public static User? UserByEmail(string email)
        {
            try
            {
                AmazonDynamoDBClient dynamoClient = new();
                DynamoDBContext dynamoContext = new(dynamoClient);

                return dynamoContext.QueryAsync<User>(email, new DynamoDBOperationConfig()
                {
                    IndexName = "email-login"
                }).GetRemainingAsync().Result.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Swallow the exception
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
            }

            return null;
        }
    }

    public class Query
    {
        public User? UserByEmailPassword(string email, string password)
        {
            try
            {
                AmazonDynamoDBClient dynamoClient = new();
                DynamoDBContext dynamoContext = new(dynamoClient);

                return dynamoContext.LoadAsync<User>(email + "|" + password).Result;
            }
            catch
            {
                // Swallow the exception
            }

            return null;
        }

        public List<Music> MusicByTitleYearArtist(string? title, int? year, string? artist)
        {
            AmazonDynamoDBClient dynamoClient = new();
            DynamoDBContext dynamoContext = new(dynamoClient);
            List<ScanCondition> scanConditions = new();

            if (year != null) // If we're searching by year
            {
                scanConditions.Add(new("Year", ScanOperator.Equal, year));
            }

            // If we're searching with by title and not artist
            if (title != null && artist == null)
            {
                return dynamoContext.QueryAsync<Music>(title, new()
                {
                    QueryFilter = scanConditions
                }).GetRemainingAsync().Result;
            }
            else if (title == null && artist != null) // If we're searching by artist and not title
            {
                return dynamoContext.QueryAsync<Music>(artist, new()
                {
                    IndexName = "music-artist",
                    QueryFilter = scanConditions
                }).GetRemainingAsync().Result;
            }
            else if (title != null && artist != null) // If we're searching by both artist and title
            {
                return dynamoContext.QueryAsync<Music>(title + "|" + artist, new()
                {
                    IndexName = "music-title-artist",
                    QueryFilter = scanConditions
                }).GetRemainingAsync().Result;
            }
            else
            { // No search criteria, return all music
                return dynamoContext.ScanAsync<Music>(scanConditions).GetRemainingAsync().Result;
            }
        }

        public List<Subscription> SubscriptionByEmail(string email)
        {
            AmazonDynamoDBClient dynamoClient = new();
            DynamoDBContext dynamoContext = new(dynamoClient);
            List<ScanCondition> scanConditions = new()
            {
                new("EMail", ScanOperator.Equal, email)
            };

            return dynamoContext.ScanAsync<Subscription>(scanConditions).GetRemainingAsync().Result;
        }
    }

    public class Mutation
    {
        public bool LoadMusic()
        {
            try
            {
                // Set up a new instance of Dynamo and S3 clients
                AmazonS3Client s3Client = new(Amazon.RegionEndpoint.APSoutheast2);
                AmazonDynamoDBClient dynamoClient = new();
                DynamoDBContext dynamoContext = new(dynamoClient);
                const string BUCKET = "cosc2639-2-s3812552";
                const string MUSIC_FILE = "a2.json";
                const string MUSIC_TABLE = "music";

                ProvisionedThroughput provisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                };

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
                                },
                                new AttributeDefinition
                                {
                                    AttributeName = "artist",
                                    AttributeType = "S"
                                },
                                new AttributeDefinition
                                {
                                    AttributeName = "title_artist",
                                    AttributeType = "S"
                                },
                                new AttributeDefinition
                                {
                                    AttributeName = "year",
                                    AttributeType = "N"
                                },
                            },
                            KeySchema = new() {
                                new KeySchemaElement
                                {
                                    AttributeName = "title",
                                    KeyType = "HASH"
                                },
                                new KeySchemaElement
                                {
                                    AttributeName = "year",
                                    KeyType = "RANGE"
                                }
                            },
                            ProvisionedThroughput = provisionedThroughput,
                            GlobalSecondaryIndexes = new() {
                                new() {
                                    IndexName = "music-artist",
                                    Projection = new() {
                                        ProjectionType = ProjectionType.ALL
                                    },
                                    KeySchema = new() {
                                        new() {
                                            AttributeName = "artist",
                                            KeyType = "HASH"
                                        },
                                        new() {
                                            AttributeName = "year",
                                            KeyType = "RANGE"
                                        }
                                    },
                                    ProvisionedThroughput = provisionedThroughput
                                },
                                new() {
                                    IndexName = "music-title-artist",
                                    Projection = new() {
                                        ProjectionType = ProjectionType.ALL
                                    },
                                    KeySchema = new() {
                                        new() {
                                            AttributeName = "title_artist",
                                            KeyType = "HASH"
                                        },
                                        new() {
                                            AttributeName = "year",
                                            KeyType = "RANGE"
                                        }
                                    },
                                    ProvisionedThroughput = provisionedThroughput
                                }
                            }
                        }).Wait();

                        // Loop until table is created
                        do
                        {
                            bool perRequestBillingActioned = false;

                            // Wait 10s for the table to be created
                            Thread.Sleep(10000);

                            // Try to change the table mode to provisioned
                            try
                            {
                                // Set the table to pay per query
                                dynamoClient.UpdateTableAsync(new UpdateTableRequest
                                {
                                    TableName = MUSIC_TABLE,
                                    BillingMode = BillingMode.PAY_PER_REQUEST
                                }).Wait();

                                // Set that we've changed the billing model
                                perRequestBillingActioned = true;
                            }
                            catch
                            {
                                // Swallow the exception
                            }

                            // Break if we have set the table to per request billing model
                            if (perRequestBillingActioned) break;
                        } while (true);
                    }

                    // Retrieve all rows from the table
                    List<ScanCondition> scanConditions = new() { };
                    List<Music> musicRows = dynamoContext.ScanAsync<Music>(scanConditions).GetRemainingAsync().Result;

                    // Delete the rows
                    foreach (Music music in musicRows)
                    {
                        dynamoContext.DeleteAsync<Music>(music.Title, music.Year).Wait();
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
                            ImgURL = imageUrl,
                            TitleArtist = song["title"].Value<string>() + "|" + song["artist"].Value<string>()
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

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return false;
        }

        public bool RegisterUser(string email, string username, string password)
        {
            AmazonDynamoDBClient dynamoClient = new();
            DynamoDBContext dynamoContext = new(dynamoClient);

            if (Helpers.UserByEmail(email) == null)
            {
                dynamoContext.SaveAsync<User>(new()
                {
                    EMailPassword = email + "|" + password,
                    EMail = email,
                    Username = username,
                    Password = password
                });

                return true;
            }

            return false;
        }

        public bool RegisterSubscription(string title, string artist, string email)
        {
            AmazonDynamoDBClient dynamoClient = new();
            DynamoDBContext dynamoContext = new(dynamoClient);
            Query query = new();
            List<Music> music = query.MusicByTitleYearArtist(title, null, artist);

            if (music.Count > 0)
            {
                Music musicItem = music.First();

                dynamoContext.SaveAsync<Subscription>(new()
                {
                    EMail = email,
                    Title = musicItem.Title,
                    Artist = musicItem.Artist,
                    Year = musicItem.Year,
                    WebURL = musicItem.WebURL,
                    ImgURL = musicItem.ImgURL,
                    TitleArtist = musicItem.TitleArtist,
                    EMailTitleArtist = email + "|" + musicItem.TitleArtist
                }).Wait();

                return true;
            }

            return false;
        }

        public bool DeleteSubscription(string title, string artist, string email)
        {
            try
            {
                AmazonDynamoDBClient dynamoClient = new();
                DynamoDBContext dynamoContext = new(dynamoClient);

                dynamoContext.DeleteAsync<Subscription>(email + "|" + title + "|" + artist, email).Wait();

                return true;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);

                return false;
            }
        }
    }

}