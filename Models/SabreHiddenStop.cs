using GetReservation;
using System.Collections.Generic;
using System.Linq;

namespace SabreWebtopTicketingService.Models
{
    public class SabreHiddenStop
    {
        private readonly AirTypeHiddenStop stop;

        public SabreHiddenStop(AirTypeHiddenStop hiddenStop)
        {
            stop = hiddenStop;
        }

        public string Airport => stop.Airport;
        public string ArrivalDateTime => stop.ArrivalDateTime;
        public string DepartureDateTime => stop.DepartureDateTime;
        public string EquipmentType => stop.EquipmentType;
        public List<string> MealCodes => stop.Meal.Select(meal => meal.Code).ToList();
    }
}