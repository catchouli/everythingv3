using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neo4j.Driver.V1;
using RaocowDotnet.Models;

namespace RaocowDotnet.Controllers {
  [Route("api/[controller]")]
  [ApiController]
  public class ChannelsController : ControllerBase {
    private readonly IDriver _driver;

    public ChannelsController(IDriver driver) {
      _driver = driver;
    }

    // GET: api/channels
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Channel>>> GetChannels() {
      return await Channel.GetChannels(_driver);
    }

    // GET: api/channels/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Channel>> GetChannel(string id) {
      var channel = await Channel.GetChannel(_driver, id);
      if (channel != null)
        return channel;
      else
        return NotFound();
    }

    // POST: api/channels
    [HttpPost]
    public async Task<ActionResult<Channel>> PostChannel(Channel item) {
      // Validate, should have no id for creating new
      if (item.Id != null)
        return BadRequest();

      // Set updated time to now
      item.Updated = DateTime.UtcNow;

      // Try saving it
      if (await item.Save(_driver))
        return CreatedAtAction(nameof(GetChannel), new { id = item.Id }, item);
      else
        return BadRequest();
    }

    // PUT: api/channels/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutChannel(string id, Channel newValues) {
      if (newValues.Id != null && newValues.Id != id)
        return BadRequest();

      // Get existing item
      Channel channel = await Channel.GetChannel(_driver, id);
      if (channel == null)
        return NotFound();

      if (newValues.Name != null)
        channel.Name = newValues.Name;
      if (newValues.YoutubeId != null)
        channel.YoutubeId = newValues.YoutubeId;
      channel.Updated = DateTime.UtcNow;

      if (await channel.Save(_driver))
        return NoContent();
      else
        return BadRequest();
    }

    // DELETE: api/channels/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChannel(string id) {
      // Get existing item
      Channel channel = await Channel.GetChannel(_driver, id);
      if (channel == null)
        return NotFound();

      if (await channel.Delete(_driver))
        return NoContent();
      else
        return BadRequest();
    }

    // GET: api/channels/5/videos
    [HttpGet("{channelId}/videos")]
    public async Task<ActionResult<IEnumerable<Video>>> GetChannelVideos(string channelId) {
      // Get existing item
      Channel channel = await Channel.GetChannel(_driver, channelId);
      if (channel == null)
        return NotFound();

      return await channel.GetVideos(_driver);
    }

    // PUT: api/channels/5/videos/id
    [HttpPut("{channelId}/videos/{videoId}")]
    public async Task<IActionResult> PutChannelVideo(string channelId, string videoId) {
      // Get existing item
      Channel channel = await Channel.GetChannel(_driver, channelId);
      if (channel == null)
        return NotFound();

      if (await channel.AddVideo(_driver, videoId))
        return NoContent();
      else
        return BadRequest();
    }

    // DELETE: api/channels/5/videos/id
    [HttpDelete("{channelId}/videos/{videoId}")]
    public async Task<IActionResult> DeleteChannelVideo(string channelId, string videoId) {
      // Get existing item
      Channel channel = await Channel.GetChannel(_driver, channelId);
      if (channel == null)
        return NotFound();

      if (await channel.RemoveVideo(_driver, videoId))
        return NoContent();
      else
        return BadRequest();
    }
  }
}
