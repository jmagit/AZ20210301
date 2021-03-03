using System;
using System.ComponentModel.DataAnnotations;

namespace Microservicios {
    /// <summary>
    /// Predicción del tiempo
    /// </summary>
    public class WeatherForecast {
        /// <summary>
        /// Identificador de la predicción
        /// </summary>
        /// <example>1</example>
        [Required]
        public int Id { get; set; }
        /// <summary>
        /// Fecha  de la predicción
        /// </summary>
        /// <example>2021-01-01T10:18:53.492Z</example>
        public DateTime Date { get; set; }
        /// <summary>
        /// Temperatura en grados centigrados
        /// </summary>
        /// <example>10</example>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperatura en grados fahrenheit
        /// </summary>
        /// <example>50</example>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// Resumen
        /// </summary>
        /// <example>Cool</example>
        public string Summary { get; set; }
    }
}
