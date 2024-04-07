using Microsoft.AspNetCore.Mvc;
using UssJuniorTest.Core;
using UssJuniorTest.Core.Models;
using UssJuniorTest.Infrastructure.Repositories;
using UssJuniorTest.Infrastructure.Store;

namespace UssJuniorTest.Controllers;

[ApiController]
[Route("api/driveLog")]
public class DriveLogController : Controller
{
    private readonly IStore _store;

    public DriveLogController(IStore store)
    {
        _store = store;
    }

    public class ReportItem
    {
        public Car? Car { get; set; }
        public Person? Driver { get; set; }
        public TimeSpan TimeOnRoad { get; set; }
    }

    private IEnumerable<ReportItem> GetTripsInfo(DateTime startDate, DateTime endDate)
    {
        var trips = _store.GetAllDriveLogs()
            .Where(t => t.StartDateTime >= startDate && t.EndDateTime <= endDate)
            .ToList();

        var driverCarInfos = trips.Select(t => new ReportItem
        {
            Car = _store.GetAllCars()
            .Where(p => p.Id == t.CarId)
            .First(),

            Driver = _store.GetAllPersons()
            .Where(p => p.Id == t.PersonId)
            .First(),

            TimeOnRoad = t.EndDateTime - t.StartDateTime
        });

        return driverCarInfos;
    }

    [HttpGet]
    public IActionResult GetDriveLogsAggregation(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate,
        [FromQuery] string driverName = "",
        [FromQuery] string carName = "",
        [FromQuery] int? pageNumber = 0,
        [FromQuery] int? pageSize = 0,
        [FromQuery] string sortBy = "")
    {
        var trips = GetTripsInfo(startDate, endDate);

        // Фильтрация
        if (!string.IsNullOrEmpty(driverName))
        {
            trips = trips.Where(t => t.Driver.Name == driverName);
        }

        if (!string.IsNullOrEmpty(carName))
        {
            trips = trips.Where(t => $"{t.Car.Manufacturer} {t.Car.Model}" == carName);
        }

        // Пагинация
        if (pageNumber != 0 && pageSize != 0)
        {
            trips = trips.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        // Сортировка
        if (!string.IsNullOrEmpty(sortBy))
        {
            if (sortBy == "personName")
                trips = trips.OrderBy(t => t.Driver.Name);

            else if (sortBy == "carName")
                trips = trips.OrderBy(t => t.Car.Manufacturer);
        }

        return Ok(trips);
    }
}