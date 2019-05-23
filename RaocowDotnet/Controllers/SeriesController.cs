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
  public class SeriesController : ControllerBase {
    private readonly IDriver _driver;

    public SeriesController(IDriver driver) {
      _driver = driver;
    }

    // temp
    public async Task GenerateTestData(IDriver driver) {
      Channel channel = new Channel { Name = "raocow", YoutubeId = "raocow", Updated = DateTime.UtcNow };
      await channel.Save(driver);
      Series series = new Series { Name = "super marisa world", Updated = DateTime.UtcNow };
      await series.Save(driver);
      for (int i = 0; i < 100; ++i) {
        Video video = new Video { Title = "Super marisa world " + i, YoutubeId = "blarg", Published = DateTime.UtcNow };
        await video.Save(driver);
        await channel.AddVideo(driver, video.Id);
        await series.AddVideo(driver, video.Id);
      }
    }

    // GET: api/series
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Series>>> GetSeries() {
      //await GenerateTestData(_driver);
      return await Series.GetSerieses(_driver);
    }

    // GET: api/series/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Series>> GetSeries(string id) {
      var series = await Series.GetSeries(_driver, id);
      if (series != null)
        return series;
      else
        return NotFound();
    }

    // POST: api/series
    [HttpPost]
    public async Task<ActionResult<Series>> PostSeries(Series item) {
      // Validate, should have no id for creating new
      if (item.Id != null)
        return BadRequest();

      // Set updated time to now
      item.Updated = DateTime.UtcNow;

      // Try saving it
      if (await item.Save(_driver))
        return CreatedAtAction(nameof(GetSeries), new { id = item.Id }, item);
      else
        return BadRequest();
    }

    // PUT: api/series/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSeries(string id, Series newValues) {
      if (newValues.Id != null && newValues.Id != id)
        return BadRequest();

      // Get existing item
      Series series = await Series.GetSeries(_driver, id);
      if (series == null)
        return NotFound();

      if (newValues.Name != null)
        series.Name = newValues.Name;
      series.Updated = DateTime.UtcNow;

      if (await series.Save(_driver))
        return NoContent();
      else
        return BadRequest();
    }

    // DELETE: api/series/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSeries(string id) {
      // Get existing item
      Series series = await Series.GetSeries(_driver, id);
      if (series == null)
        return NotFound();

      if (await series.Delete(_driver))
        return NoContent();
      else
        return BadRequest();
    }

    // GET: api/series/5/videos
    [HttpGet("{seriesId}/videos")]
    public async Task<ActionResult<IEnumerable<Video>>> GetSeriesVideos(string seriesId) {
      // Get existing item
      Series series = await Series.GetSeries(_driver, seriesId);
      if (series == null)
        return NotFound();

      return await series.GetVideos(_driver);
    }

    // PUT: api/series/5/videos/id
    [HttpPut("{seriesId}/videos/{videoId}")]
    public async Task<IActionResult> PutSeriesVideo(string seriesId, string videoId) {
      // Get existing item
      Series series = await Series.GetSeries(_driver, seriesId);
      if (series == null)
        return NotFound();

      if (await series.AddVideo(_driver, videoId))
        return NoContent();
      else
        return BadRequest();
    }

    // DELETE: api/series/5/videos/id
    [HttpDelete("{seriesId}/videos/{videoId}")]
    public async Task<IActionResult> DeleteSeriesVideo(string seriesId, string videoId) {
      // Get existing item
      Series series = await Series.GetSeries(_driver, seriesId);
      if (series == null)
        return NotFound();

      if (await series.RemoveVideo(_driver, videoId))
        return NoContent();
      else
        return BadRequest();
    }
  }
}
