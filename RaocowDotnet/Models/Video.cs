using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RaocowDotnet.Models {
  public class Video {
    public string Id { get; private set; }
    public string YoutubeId { get; set; }
    public string Title { get; set; }
    public DateTime? Published { get; set; }

    /// <summary>
    ///  Get videos from the database
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static async Task<List<Video>> GetVideos(IDriver driver) {
      var statement = "MATCH (n:Video) RETURN n";
      using (var session = driver.Session()) {
        return await session.ReadTransactionAsync(async tx => {
          var result = await tx.RunAsync(statement);
          return await result.ToListAsync<Video>(record => {
            var node = record[0].As<INode>();
            return new Video {
              Id = node.Properties.GetValueOrDefault("id", null).As<string>(),
              YoutubeId = node.Properties.GetValueOrDefault("youtubeId", null).As<string>(),
              Title = node.Properties.GetValueOrDefault("title", null).As<string>(),
              Published = node.Properties.GetValueOrDefault("published", null).As<DateTimeOffset?>()?.UtcDateTime
            };
          });
        });
      }
    }

    /// <summary>
    ///  Get a single video by id
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static async Task<Video> GetVideo(IDriver driver, string id) {
      var statement = "MATCH (n:Video) where n.id = $id RETURN n.youtubeId as youtubeId, n.title as title, n.published as published";
      using (var session = driver.Session()) {
        return await session.ReadTransactionAsync(async tx => {
          var result = await tx.RunAsync(statement, new { id });
          if (await result.FetchAsync()) {
            return new Video {
              Id = id,
              YoutubeId = result.Current["youtubeId"].As<string>(),
              Title = result.Current["title"].As<string>(),
              Published = result.Current["published"].As<DateTimeOffset?>()?.UtcDateTime
            };
          }
          else {
            return null;
          }
        });
      }
    }

    /// <summary>
    ///  Save this video to the database
    /// </summary>
    /// <param name="driver"></param>
    public async Task<bool> Save(IDriver driver) {
      // Validate values
      if (Title == null || Published == null || YoutubeId == null)
        return false;

      // Write to database
      using (var session = driver.Session()) {
        return await session.WriteTransactionAsync(async tx => {
          string id = Id;
          var newItem = (id == null);

          // Generate an id from the title and check it's free
          int suffix = 0;
          while (id == null) {
            // Find free ID
            string idPrefix = GenerateID();
            id = idPrefix + (suffix > 0 ? suffix.ToString() : "");
            var res = await tx.RunAsync("MATCH (video:Video) WHERE video.id = $Id RETURN video.id", new { Id = id });

            // If the id is already in use try adding another suffix
            if (await res.FetchAsync()) {
              id = null;
              ++suffix;
            }
          }

          // If it's a new record we need to create it, otherwise just update it
          var statement = newItem ? "CREATE (video:Video)" : "MATCH (video:Video) WHERE video.id = $Id";
          statement += " SET video.id = $Id, video.title = $Title, video.published = $Published, video.youtubeId = $YoutubeId";

          try {
            await tx.RunAsync(statement, new { Id = id, Title, Published, YoutubeId });
            this.Id = id;
            return true;
          }
          catch (Exception) {
            return false;
          }
        });
      }
    }

    /// <summary>
    ///  Delete this video from the database
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public async Task<bool> Delete(IDriver driver) {
      if (Id == null)
        return false;

      using (var session = driver.Session()) {
        return await session.WriteTransactionAsync(async tx => {
          try {
            await tx.RunAsync("MATCH (n:Video) where n.id = $Id DETACH DELETE n", new { Id });
            Id = null;
            return true;
          }
          catch (Exception) {
            return false;
          }
        });
      }
    }

    /// <summary>
    ///  Get a list of videos by channel id
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static async Task<List<Video>> GetVideosByChannel(IDriver driver, string channelId) {
      var statement = "MATCH (c:Channel)-[:HAS_VIDEO]->(v:Video) where c.id = $channelId RETURN v";
      using (var session = driver.Session()) {
        return await session.ReadTransactionAsync(async tx => {
          var result = await tx.RunAsync(statement, new { channelId });
          return await result.ToListAsync<Video>(record => {
            var node = record[0].As<INode>();
            return new Video {
              Id = node.Properties.GetValueOrDefault("id", null).As<string>(),
              YoutubeId = node.Properties.GetValueOrDefault("youtubeId", null).As<string>(),
              Title = node.Properties.GetValueOrDefault("title", null).As<string>(),
              Published = node.Properties.GetValueOrDefault("published", null).As<DateTimeOffset?>()?.UtcDateTime
            };
          });
        });
      }
    }

    /// <summary>
    ///  Get a list of videos by series id
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static async Task<List<Video>> GetVideosBySeries(IDriver driver, string seriesId) {
      var statement = "MATCH (s:Series)-[:HAS_VIDEO]->(v:Video) where s.id = $seriesId RETURN v";
      using (var session = driver.Session()) {
        return await session.ReadTransactionAsync(async tx => {
          var result = await tx.RunAsync(statement, new { seriesId });
          return await result.ToListAsync<Video>(record => {
            var node = record[0].As<INode>();
            return new Video {
              Id = node.Properties.GetValueOrDefault("id", null).As<string>(),
              YoutubeId = node.Properties.GetValueOrDefault("youtubeId", null).As<string>(),
              Title = node.Properties.GetValueOrDefault("title", null).As<string>(),
              Published = node.Properties.GetValueOrDefault("published", null).As<DateTimeOffset?>()?.UtcDateTime
            };
          });
        });
      }
    }

    /// <summary>
    ///  Generate a url-safe id, the caller needs to check if it's unique before using it
    /// </summary>
    /// <returns></returns>
    string GenerateID() {
      const int maxLength = 64;
      string id = Regex.Replace(Title, @"[\W]+", "-").ToLower().TrimStart('-').TrimEnd('-');
      if (id.Length > maxLength)
        id = id.Substring(0, maxLength);
      return id;
    }
  }
}
