using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RG_PZ2.Models
{
    public class LineEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsUnderground { get; set; }
        public float R { get; set; }
        public string ConductorMaterial { get; set; }
        public string LineType { get; set; }
        public long ThermalConstantHeat { get; set; }
        public long FirstEnd { get; set; }
        public long SecondEnd { get; set; }
        public List<Vertex> Vertices { get; set; }
    }
}
