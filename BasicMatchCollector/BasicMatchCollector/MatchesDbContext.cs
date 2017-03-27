using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicMatchCollector
{
    public class MatchesDbContext : DbContext
    {
        //public DbSet<Match> Matches { get; set; }

        public MatchesDbContext() : base("name = MatchesDbConnection") { }
    }
}
