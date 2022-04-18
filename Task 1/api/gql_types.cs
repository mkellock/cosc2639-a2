using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace api
{

    [DynamoDBTable("music")]
    public class Music
    {
        [DynamoDBHashKey]
        public string Title { get; set; }

        [DynamoDBProperty]
        public string Artist { get; set; }

        [DynamoDBProperty]
        public short Year { get; set; }

        [DynamoDBProperty]
        public string WebURL { get; set; }

        [DynamoDBProperty]
        public string ImgURL { get; set; }
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
                    // If the table exists, drop it
                    if (dynamoClient.ListTablesAsync().Result.TableNames.Contains(MUSIC_TABLE))
                    {
                        // Delete the table
                        dynamoClient.DeleteTableAsync(MUSIC_TABLE).Wait();

                        // Loop until table is deleted
                        do
                        {
                            // Wait 1s for the table to be deleted
                            Thread.Sleep(1000);
                        } while (dynamoClient.ListTablesAsync().Result.TableNames.Contains(MUSIC_TABLE));
                    }

                    // Recreate the table
                    dynamoClient.CreateTableAsync(new()
                    {
                        TableName = MUSIC_TABLE,
                        AttributeDefinitions = new() {
                                new AttributeDefinition
                                {
                                    AttributeName = "Title",
                                    AttributeType = "S"
                                }
                            },
                        KeySchema = new() {
                                new KeySchemaElement
                                {
                                    AttributeName = "Title",
                                    KeyType = "HASH"
                                }
                            },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 10,
                            WriteCapacityUnits = 10
                        },
                    }).Wait();

                    // Loop until table is created
                    do
                    {
                        // Wait 5s for the table to be created
                        Thread.Sleep(5000);
                    } while (!dynamoClient.ListTablesAsync().Result.TableNames.Contains(MUSIC_TABLE));

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
                            Year = song["year"].Value<Int16>(),
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
    }

    // public class Mutation
    // {

    // }

}