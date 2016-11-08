# playlistyoutube
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Google.Apis.YouTube.Samples
{
    /// <summary>
    /// YouTube Data API v3 sample: search by keyword.
    /// Relies on the Google APIs Client Library for .NET, v1.7.0 or higher.
    /// See https://code.google.com/p/google-api-dotnet-client/wiki/GettingStarted
    ///
    /// Set ApiKey to the API key value from the APIs & auth > Registered apps tab of
    ///   https://cloud.google.com/console
    /// Please ensure that you have enabled the YouTube Data API for your project.
    /// </summary>
    internal class Search
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("YouTube Data API: Search");
            Console.WriteLine("========================");

            try
            {
               
                new Search().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {
        
        //Load lít keyword tu file kyword.csv
            var reader = new StreamReader(File.OpenRead("keyword.csv"));
            
            var listKey = reader.ReadToEnd().Split('.');
            Console.WriteLine("co {0} key ", listKey.Length);
            foreach (string a in listKey) Console.WriteLine(a);
            UserCredential credential;

//load api tu file client_id 
            using (var stream = new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for full read/write access to the
                    // authenticated user's account.
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });
            var channelsListRequest = youtubeService.Channels.List("contentDetails");
            Console.Write("Channel ID:");
            channelsListRequest.Id = Console.ReadLine();

            // Retrieve the contentDetails part of the channel resource for the authenticated user's channel.
            var channelsListResponse = await channelsListRequest.ExecuteAsync();
            List<string> videosMine = new List<string>();
            // Console.WriteLine(channelsListResponse.Items.Count);
            foreach (var channel in channelsListResponse.Items)
            {
                // From the API response, extract the playlist ID that identifies the list
                // of videos uploaded to the authenticated user's channel.
                var uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;

                // Console.WriteLine("Videos in channel: {0}", uploadsListId);

                var nextPageToken = "";
                while (nextPageToken != null)
                {
                    var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                    playlistItemsListRequest.PlaylistId = uploadsListId;
                    playlistItemsListRequest.MaxResults = 50;
                    playlistItemsListRequest.PageToken = nextPageToken;

                    // Retrieve the list of videos uploaded to the authenticated user's channel.
                    var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                    foreach (var playlistItem in playlistItemsListResponse.Items)
                    {
                        // Print information about each video.

                        videosMine.Add(playlistItem.Snippet.ResourceId.VideoId);
                    }

                    nextPageToken = playlistItemsListResponse.NextPageToken;
                }
            }
            Console.WriteLine("Da them {0} vao danh sach!",videosMine.Count);
            
            
           // List<Playlist> listIDPLL = new List<Playlist>();
            foreach (var key in listKey)
            {
                    //create new playlis
                    var playList = new Playlist();
                    playList.Snippet = new PlaylistSnippet();
                    playList.Snippet.Title = key + " 2016 new version";
                    playList.Snippet.Description = playList.Snippet.Title;
                    playList.Status = new PlaylistStatus();
                    playList.Status.PrivacyStatus = "public";
                try
                {
                    playList = await youtubeService.Playlists.Insert(playList, "snippet,status").ExecuteAsync();
                    Console.WriteLine(playList.Id);
                }catch
                {

                    goto endRespone;
                }
                //listIDPLL.Add(playList);
            //}
               //foreach(var playList in listIDPLL) { 
                List<string> videos = new List<string>();
                Random rnd = new Random();
                int limit = rnd.Next(5, 10);
                var nextPageToken = "";
                while (nextPageToken != null)
                {
                    var searchListRequest = youtubeService.Search.List("snippet");
                    //searchListRequest.Q = playList.Snippet.Title;
                    searchListRequest.Q = playList.Snippet.Title;
                    searchListRequest.MaxResults = 50;
                    searchListRequest.PageToken = nextPageToken;

                    var searchListResponse  = await searchListRequest.ExecuteAsync();

                    
                    foreach (var searchResult in searchListResponse.Items)
                    {
                        
                        if (searchResult.Id.Kind == "youtube#video")
                        {
                            videos.Add(searchResult.Id.VideoId);
                            if (videos.Count > limit)
                            {
                                goto next;
                            }
                        }
                    }
                   
                    nextPageToken = searchListResponse.NextPageToken;
                }
                next:
                Console.WriteLine("them search vao pll");
                //Them tat ca video tim thay vao playlist
                foreach (var videoID in videos)
                {try
                    {
                        Console.WriteLine(videoID);
                        var newPlaylistVideo = new PlaylistItem();
                        newPlaylistVideo.Snippet = new PlaylistItemSnippet();
                        newPlaylistVideo.Snippet.PlaylistId = playList.Id;
                        newPlaylistVideo.Snippet.ResourceId = new ResourceId();
                        newPlaylistVideo.Snippet.ResourceId.Kind = "youtube#video";
                        newPlaylistVideo.Snippet.ResourceId.VideoId = videoID;
                        newPlaylistVideo = await youtubeService.PlaylistItems.Insert(newPlaylistVideo, "snippet").ExecuteAsync();
                        
                    }
                    catch
                    {

                        Console.WriteLine("Video lol trau deo add dc");
                    }
                }
                Console.WriteLine("them vao #2");
                //them tat ca video cua channel id vao playlist index #2
                foreach (var videoMineID in videosMine)
                {
                    try
                    {
                        Console.WriteLine(videoMineID);
                        var newPlaylistitem = new PlaylistItem();
                        newPlaylistitem.Snippet = new PlaylistItemSnippet();
                        newPlaylistitem.Snippet.PlaylistId = playList.Id;
                        newPlaylistitem.Snippet.ResourceId = new ResourceId();
                        newPlaylistitem.Snippet.ResourceId.Kind = "youtube#video";
                        newPlaylistitem.Snippet.ResourceId.VideoId = videoMineID;
                        newPlaylistitem.Snippet.Position = 0;
                        newPlaylistitem = await youtubeService.PlaylistItems.Insert(newPlaylistitem, "snippet").ExecuteAsync();
                    }catch
                    {
                        Console.WriteLine("LOL trau");
                    }
                }
                Console.WriteLine("Themv vao #1");
                //them video 1s vao index #1
                try
                {
                    var newPlaylistitem2 = new PlaylistItem();
                    newPlaylistitem2.Snippet = new PlaylistItemSnippet();
                    newPlaylistitem2.Snippet.PlaylistId = playList.Id;
                    newPlaylistitem2.Snippet.ResourceId = new ResourceId();
                    newPlaylistitem2.Snippet.ResourceId.Kind = "youtube#video";
                    newPlaylistitem2.Snippet.ResourceId.VideoId = "id cua 1 video ";
                    newPlaylistitem2.Snippet.Position = 0;
                    newPlaylistitem2 = await youtubeService.PlaylistItems.Insert(newPlaylistitem2, "snippet").ExecuteAsync();
                }
                catch
                {
                    Console.WriteLine(">.<");
                }
                // Console.WriteLine("Check Playlist ID:{0}", newPlaylistVideo.Snippet.PlaylistId);
                Console.WriteLine("Xong key {0},id {1}", playList.Snippet.Title, playList.Id);
                endRespone:
                Console.WriteLine();
            }

            


        }
    }
}
