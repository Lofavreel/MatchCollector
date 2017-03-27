using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicMatchCollector
{
    public class BasicMatchDetails
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long MatchId { get; set; }
        public string MatchVersion { get; set; }
        public bool Winner { get; set; } // true == blue won
        
        public int BlueTopChampId { get; set; }
        public int BlueJungleChampId { get; set; }
        public int BlueMiddleChampId { get; set; }
        public int BlueCarryChampId { get; set; } = 0;
        public int BlueSupportChampId { get; set; }

        public int PurpleTopChampId { get; set; }
        public int PurpleJungleChampId { get; set; }
        public int PurpleMiddleChampId { get; set; }
        public int PurpleCarryChampId { get; set; } = 0;
        public int PurpleSupportChampId { get; set; }
        
    }
}
