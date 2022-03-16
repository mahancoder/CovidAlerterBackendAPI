using System;

namespace BackendAPI.Models
{
    public class ScoreLog
    {
        public int Id {get; set;}
        public int NeighbourhoodId {get; set;}
        public Neighbourhood Neighbourhood {get; set;}
        public float Score {get; set;}
        public DateTime Date { get; set; }
    }
}