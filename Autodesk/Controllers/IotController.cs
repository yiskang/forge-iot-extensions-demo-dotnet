/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using JsonFlatFileDataStore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Autodesk.Forge.Models;
using Autodesk.Forge.Libs;

namespace Autodesk.Forge.Controllers
{
    [ApiController]
    public class IotController : ControllerBase
    {
        private readonly IDataStore dataStore;

        public IotController(IDataStore dataStore)
        {
            this.dataStore = dataStore;
        }

        [HttpGet]
        [Route("iot/samples")]
        public async Task<IActionResult> GetSamplesAsync([FromQuery] string? start, [FromQuery] string? end, [FromQuery] double? resolution)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end) || !resolution.HasValue)
                    throw new InvalidDataException("Missing some of the required parameters: \"start\", \"end\", \"resolution\".");

                var sensors = this.dataStore.GetCollection<Sensor>();

                var startTime = DateTime.Parse(start, null, System.Globalization.DateTimeStyles.RoundtripKind);
                var endTime = DateTime.Parse(end, null, System.Globalization.DateTimeStyles.RoundtripKind);

                double resol = resolution.HasValue ? resolution.Value : 0;
                var result = Utility.GetSamples(sensors.AsQueryable(), startTime, endTime, resol);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Route("iot/channels")]
        public async Task<IActionResult> GetChannelsAsync()
        {
            try
            {
                var channels = this.dataStore.GetCollection<Channel>();
                return Ok(channels.AsQueryable());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        [Route("iot/sensors")]
        public async Task<IActionResult> GetSensorsAsync([FromQuery] string? code)
        {
            try
            {
                var data = this.dataStore.GetCollection<Sensor>();
                var sensors = data.AsQueryable();

                if (!string.IsNullOrWhiteSpace(code))
                    return Ok(sensors.Where(s => s.Code == code));

                return Ok(sensors);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("iot/sensors")]
        public async Task<IActionResult> AddSensorsAsync([FromBody] Sensor data)
        {
            try
            {
                var sensors = this.dataStore.GetCollection<Sensor>();
                await sensors.InsertOneAsync(data);

                sensors = this.dataStore.GetCollection<Sensor>();
                var sensor = sensors.AsQueryable().FirstOrDefault(s => s.Code == data.Code);
                return Ok(sensor);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpDelete]
        [Route("iot/sensors/{id}")]
        public async Task<IActionResult> DeleteSensorsAsync(int id)
        {
            try
            {
                var sensors = this.dataStore.GetCollection<Sensor>();
                var sensor = sensors.AsQueryable().FirstOrDefault(s => s.Id == id);
                if (sensor == null)
                    return NotFound();

                await sensors.DeleteOneAsync(id);
                return Ok(sensor);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}