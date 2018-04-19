using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Front.Domain.Entites;
namespace Front.WebUI.Models.Game
{
    public class UnitDataModel
    {       
           public IEnumerable<UnitsByTypes> AllUnits { get; set; }
           public IEnumerable<Unit> MyUnits { get; set; }
    }
    public class UnitsByTypes
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Count { get; set; }
        public string Type { get; set; }
        public string Land { get; set; }

    }
}