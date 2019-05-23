using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RaocowDotnet.Models {
  public class Channel {
    public string Id { get; set; }
    public string YoutubeId { get; set; }
    public string Name { get; set; }
    public DateTime? Updated { get; set; }

    /// <summary>
    ///  Get channel from the database
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static async Task<List<Channel>> GetChannels(IDriver driver) {
      var statement = "MATCH (n:Channel) RETURN n";
      using (var session = driver.Session()) {
        return await session.ReadTransactionAsync(async tx => {
          var result = await tx.RunAsync(statement);
          return await result.ToListAsync<Channel>(record => {
            var node = record[0].As<INode>();
            return new Channel {
              Id = node.Properties.GetValueOrDefault("id", null).As<string>(),
              YoutubeId = node.Properties.GetValueOrDefault("youtubeId", null).As<string>(),
              Name = node.Properties.GetValueOrDefault("name", null).As<string>(),
              Updated = node.Properties.GetValueOrDefault("updated", null).As<DateTimeOffset?>()?.UtcDateTime
            };
          });
        });
      }
    }

    /// <summary>
    ///  Get a single channel by id
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public static async Task<Channel> GetChannel(IDriver driver, string id) {
      var statement = "MATCH (n:Channel) where n.id = $id RETURN n.youtubeId as youtubeId, n.name as name, n.updated as updated";
      using (var session = driver.Session()) {
        return await session.ReadTransactionAsync(async tx => {
          var result = await tx.RunAsync(statement, new { id });
          if (await result.FetchAsync()) {
            return new Channel {
              Id = id,
              YoutubeId = result.Current["youtubeId"].As<string>(),
              Name = result.Current["name"].As<string>(),
              Updated = result.Current["updated"].As<DateTimeOffset?>()?.UtcDateTime
            };
          }
          else {
            return null;
          }
        });
      }
    }

    /// <summary>
    ///  Save this channel to the database
    /// </summary>
    /// <param name="driver"></param>
    public async Task<bool> Save(IDriver driver) {
      // Validate values
      if (YoutubeId == null || Name == null)
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
            var res = await tx.RunAsync("MATCH (channel:Channel) WHERE channel.id = $Id RETURN channel.id", new { Id = id });

            // If the id is already in use try adding another suffix
            if (await res.FetchAsync()) {
              id = null;
              ++suffix;
            }
          }

          // If it's a new record we need to create it, otherwise just update it
          var statement = newItem ? "CREATE (channel:Channel)" : "MATCH (channel:Channel) WHERE channel.id = $Id";
          statement += " SET channel.id = $Id, channel.name = $Name, channel.updated = $Updated, channel.youtubeId = $YoutubeId";

          try {
            await tx.RunAsync(statement, new { Id = id, Name, Updated, YoutubeId });
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
    ///  Delete this channel from the database
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public async Task<bool> Delete(IDriver driver) {
      if (Id == null)
        return false;

      using (var session = driver.Session()) {
        return await session.WriteTransactionAsync(async tx => {
          try {
            await tx.RunAsync("MATCH (n:Channel) where n.id = $Id DETACH DELETE n", new { Id });
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
    ///  Get a list of videos by a channel
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public async Task<List<Video>> GetVideos(IDriver driver) {
      return await Video.GetVideosByChannel(driver, Id);
    }

    /// <summary>
    ///  Add a video to a channel
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public async Task<bool> AddVideo(IDriver driver, string videoId) {
      if (Id == null)
        return false;

      using (var session = driver.Session()) {
        return await session.WriteTransactionAsync(async tx => {
          try {
            var statement = "MATCH (v:Video) WHERE v.id = $videoId MATCH (c:Channel) WHERE c.id = $channelId CREATE UNIQUE (c)-[:HAS_VIDEO]->(v)";
            await tx.RunAsync(statement, new { channelId = Id, videoId });
            return true;
          }
          catch (Exception) {
            return false;
          }
        });
      }
    }

    /// <summary>
    ///  Removes a video to a channel
    /// </summary>
    /// <param name="driver"></param>
    /// <returns></returns>
    public async Task<bool> RemoveVideo(IDriver driver, string videoId) {
      if (Id == null)
        return false;

      using (var session = driver.Session()) {
        return await session.WriteTransactionAsync(async tx => {
          try {
            var statement = "MATCH (c:Channel)-[rel:HAS_VIDEO]->(v:Video) WHERE c.id = $channelId AND v.id = $videoId DELETE rel";
            await tx.RunAsync(statement, new { channelId = Id, videoId });
            return true;
          }
          catch (Exception) {
            return false;
          }
        });
      }
    }

    /// <summary>
    ///  Generate a url-safe id, the caller needs to check if it's unique before using it
    /// </summary>
    /// <returns></returns>
    string GenerateID() {
      const int maxLength = 64;
      string id = Regex.Replace(Name, @"[\W]+", "-").ToLower().TrimStart('-').TrimEnd('-');
      if (id.Length > maxLength)
        id = id.Substring(0, maxLength);
      return id;
    }
  }
}
