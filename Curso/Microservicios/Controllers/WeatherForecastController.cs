using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microservicios.Controllers {
    /// <summary>
    /// Encapsula los mensajes de error
    /// </summary>
    public class ErrorMessage {
        /// <summary>
        /// Causa del error
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Descripción del error
        /// </summary>
        public string Description { get; set; }
        public ErrorMessage(string message) {
            Message = message;
        }
        public ErrorMessage(string message, string description) : this(message) {
            Description = description;
        }
    }

    /// <summary>
    /// Mantenimiento de predicciones
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        private static IList<WeatherForecast> dbSet;
        static WeatherForecastController() {
            var rng = new Random();
            dbSet = Enumerable.Range(1, 5).Select(index => new WeatherForecast {
                Id = index,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToList();
        }

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger) {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> GetAllWeatherForecast() {
            return dbSet;
        }

        /// <summary>
        /// Consulta una prediccion
        /// </summary>
        /// <param name="id">Código de la predicción</param>
        /// <returns>Detalles de la prediccion</returns>
        /// <response code="200">Predicción encontrada</response>
        /// <response code="404">Predicción no encontrada</response>            
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<WeatherForecast> GetWeatherForecast(int id) {
            var entity = dbSet.FirstOrDefault(item => item.Id == id);
            if (entity == null)
                return NotFound();
            this.HttpContext.Response.Headers.Add("Cache-Control", "private,max-age=10");
            this.HttpContext.Response.Headers.Add("ETag", entity.GetHashCode().ToString());
            if (this.HttpContext.Request.Headers["If-None-Match"] == entity.GetHashCode().ToString())
                return this.StatusCode(304);
            return dbSet.First(item => item.Id == id);
        }

        /// <summary>
        /// Crea una nueva predicción
        /// </summary>
        /// <param name="value">Detalles de la predicción</param>
        /// <returns>Codigo de estado</returns>
        /// <response code="201">Predicción creada</response>
        /// <response code="400">Clave duplicada</response>            
        /// <response code="409">Problemas de persistencia</response>            
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorMessage))]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorMessage))]
        public IActionResult PostWeatherForecast([FromBody] WeatherForecast value) {
            var entity = dbSet.FirstOrDefault(item => item.Id == value.Id);
            if (entity != null)
                return BadRequest(new ErrorMessage("Duplicate key"));
            entity = dbSet.LastOrDefault();
            value.Id = entity == null ? 0 : entity.Id + 1;
            try {
                dbSet.Add(value);
            } catch (Exception ex) {
                return this.Conflict(new ErrorMessage("Error de creacion", ex.Message));
            }
            return CreatedAtAction("GetWeatherForecast", new { id = entity.Id }, entity);
        }

        /// <summary>
        /// Modificación de la prediccion
        /// </summary>
        /// <param name="id">Código de la predicción</param>
        /// <param name="value">Detalles de la predicción</param>
        /// <returns></returns>
        /// <response code="204">Predicción modificado</response>
        /// <response code="400">Error en el identificador</response>            
        /// <response code="404">Predicción no encontrada</response>            
        /// <response code="409">Problemas de persistencia</response>            
        /// <response code="412">Problemas de concurrencia</response>            
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorMessage))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorMessage))]
        [ProducesResponseType(StatusCodes.Status412PreconditionFailed, Type = typeof(ErrorMessage))]
        public IActionResult PutWeatherForecast(int id, [FromBody] WeatherForecast value) {
            if (id != value.Id)
                return BadRequest(new ErrorMessage("Invalid key"));
            var entity = dbSet.FirstOrDefault(item => item.Id == id);
            if (entity == null)
                return NotFound();
            if (this.HttpContext.Request.Headers.ContainsKey("If-Match") &&
                this.HttpContext.Request.Headers["If-Match"] != entity.GetHashCode().ToString())
                return this.StatusCode(412, new ErrorMessage("Error de concurrencia", "Los datos se han modificado desde que se descargaron"));
            try {
                dbSet[dbSet.IndexOf(entity)] = value;
            } catch (Exception ex) {
                return this.Conflict(new ErrorMessage("Error de actualizacion", ex.Message));
            }
            return NoContent();
        }

        /// <summary>
        /// Borrado de la predicción
        /// </summary>
        /// <param name="id">Código de la predicción</param>
        /// <returns></returns>
        /// <response code="204">Predicción eliminada</response>
        /// <response code="404">Predicción no encontrada</response>            
        /// <response code="409">Problemas de persistencia</response>            
        /// <response code="412">Problemas de concurrencia</response>            
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorMessage))]
        [ProducesResponseType(StatusCodes.Status412PreconditionFailed, Type = typeof(ErrorMessage))]
        public IActionResult IDeleteWeatherForecast(int id) {
            var entity = dbSet.FirstOrDefault(item => item.Id == id);
            if (entity == null)
                return NotFound();
            if (this.HttpContext.Request.Headers.ContainsKey("If-Match") &&
                this.HttpContext.Request.Headers["If-Match"] != entity.GetHashCode().ToString())
                return this.StatusCode(412, new ErrorMessage("Error de concurrencia", "Los datos se han modificado desde que se descargaron"));
            try {
                dbSet.Remove(entity);
            } catch (Exception ex) {
                return this.Conflict(new ErrorMessage("Error de borrado", ex.Message));
            }
            return NoContent();
        }

    }
}
