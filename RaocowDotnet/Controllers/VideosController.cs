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
  public class VideosController : ControllerBase {
    private readonly IDriver _driver;

    public VideosController(IDriver driver) {
      _driver = driver;
    }

    // GET: api/videos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Video>>> GetVideos() {
      return await Video.GetVideos(_driver);
    }

    // GET: api/videos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Video>> GetVideo(string id) {
      var video = await Video.GetVideo(_driver, id);
      if (video != null)
        return video;
      else
        return NotFound();
    }

    // POST: api/videos
    [HttpPost]
    public async Task<ActionResult<Video>> PostVideo(Video item) {
      // Validate, should have no id for creating new
      if (item.Id != null)
        return BadRequest();

      // Try saving it
      if (await item.Save(_driver))
        return CreatedAtAction(nameof(GetVideo), new { id = item.Id }, item);
      else
        return BadRequest();
    }

    // PUT: api/videos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutVideo(string id, Video newValues) {
      if (newValues.Id != null && newValues.Id != id)
        return BadRequest();

      // Get existing item
      Video video = await Video.GetVideo(_driver, id);
      if (video == null)
        return NotFound();

      if (newValues.Title != null)
        video.Title = newValues.Title;
      if (newValues.YoutubeId != null)
        video.YoutubeId = newValues.YoutubeId;
      if (newValues.Published != null)
        video.Published = newValues.Published;

      if (await video.Save(_driver))
        return NoContent();
      else
        return BadRequest();
    }

    // DELETE: api/videos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVideo(string id) {
      // Get existing item
      Video video = await Video.GetVideo(_driver, id);
      if (video == null)
        return NotFound();

      if (await video.Delete(_driver))
        return NoContent();
      else
        return BadRequest();
    }
  }
}
